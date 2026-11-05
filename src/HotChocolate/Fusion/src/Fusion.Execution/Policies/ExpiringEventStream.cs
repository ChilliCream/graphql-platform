using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Policies.Audit;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// Ends the results of a subscription gracefully when the token of the principal expires.
/// </summary>
internal sealed class ExpiringEventStream : IAsyncEnumerable<OperationResult>
{
    private const string ExpiredReason = "expired";

    private readonly IAsyncEnumerable<OperationResult> _results;
    private readonly IExecutionResult _result;
    private readonly ClaimsPrincipal _user;
    private readonly PolicyEvaluator _evaluator;
    private readonly IAuditTrail _trail;
    private readonly AuditScopeInfo _info;
    private readonly bool _recordsAudit;
    private readonly OperationPlan _plan;
    private readonly IVariableValueCollection _variables;
    private readonly DateTimeOffset _expiry;
    private readonly TimeProvider _timeProvider;
    private readonly IErrorHandler _errorHandler;
    private readonly Action<Exception> _reportFailure;

    /// <summary>
    /// Initializes a new instance of <see cref="ExpiringEventStream"/>.
    /// </summary>
    /// <param name="results">
    /// The results of the subscription.
    /// </param>
    /// <param name="result">
    /// The result that delivers the results and requests 401 Unauthorized when the token expires.
    /// </param>
    /// <param name="user">
    /// The principal of the subscription.
    /// </param>
    /// <param name="evaluator">
    /// The evaluator that records the denied entries of the expiry.
    /// </param>
    /// <param name="scope">
    /// The audit scope of the decision made when the subscription started.
    /// </param>
    /// <param name="plan">
    /// The plan of the subscription.
    /// </param>
    /// <param name="variables">
    /// The coerced variable values of the subscription without the synthetic variables.
    /// </param>
    /// <param name="expiry">
    /// The time at which the token of the principal expires.
    /// </param>
    /// <param name="timeProvider">
    /// The time provider that measures the expiry of the token.
    /// </param>
    /// <param name="errorHandler">
    /// The error handler that shapes the error result of a failed expiry audit scope.
    /// </param>
    /// <param name="reportFailure">
    /// The callback that receives the failure to commit the audit scope of the expiry.
    /// </param>
    public ExpiringEventStream(
        IAsyncEnumerable<OperationResult> results,
        IExecutionResult result,
        ClaimsPrincipal user,
        PolicyEvaluator evaluator,
        IAuditScope scope,
        OperationPlan plan,
        IVariableValueCollection variables,
        DateTimeOffset expiry,
        TimeProvider timeProvider,
        IErrorHandler errorHandler,
        Action<Exception> reportFailure)
    {
        _results = results;
        _result = result;
        _user = user;
        _evaluator = evaluator;
        _trail = scope.Trail;
        _info = scope.Info;
        _recordsAudit = scope.IsRecording;
        _plan = plan;
        _variables = variables;
        _expiry = expiry;
        _timeProvider = timeProvider;
        _errorHandler = errorHandler;
        _reportFailure = reportFailure;
    }

    public IAsyncEnumerator<OperationResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new Enumerator(this, cancellationToken);

    private async ValueTask<Exception?> RecordExpiryAsync(CancellationToken cancellationToken)
    {
        if (!_recordsAudit)
        {
            return null;
        }

        try
        {
            var scope = _trail.BeginRequest(_info, _user);

            _evaluator.RecordDenied(scope, _plan, _variables, ExpiredReason);
            await scope.CommitAsync(cancellationToken);
            return null;
        }
        catch (Exception ex) when (PolicyEvaluator.IsFault(ex, cancellationToken))
        {
            return ex;
        }
    }

    private sealed class Enumerator : IAsyncEnumerator<OperationResult>
    {
        private readonly ExpiringEventStream _stream;
        private readonly CancellationToken _callerToken;
        private readonly CancellationTokenSource _expired;
        private readonly CancellationTokenSource _linked;
        private readonly IAsyncEnumerator<OperationResult> _inner;
        private bool _ended;

        public Enumerator(ExpiringEventStream stream, CancellationToken cancellationToken)
        {
            var remaining = stream._expiry - stream._timeProvider.GetUtcNow();

            _stream = stream;
            _callerToken = cancellationToken;
            _expired = new CancellationTokenSource(
                remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero,
                stream._timeProvider);
            _linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _expired.Token);
            _inner = stream._results.GetAsyncEnumerator(_linked.Token);
        }

        public OperationResult Current { get; private set; } = null!;

        private bool IsExpired => _expired.IsCancellationRequested && !_callerToken.IsCancellationRequested;

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public async ValueTask<bool> MoveNextAsync()
        {
            if (_ended)
            {
                return false;
            }

            bool hasNext;

            try
            {
                hasNext = await _inner.MoveNextAsync();
            }
            catch (OperationCanceledException) when (IsExpired)
            {
                hasNext = false;
            }

            if (hasNext)
            {
                Current = _inner.Current;
                return true;
            }

            _ended = true;

            if (!IsExpired)
            {
                return false;
            }

            _stream._result.ContextData = _stream._result.ContextData.SetItem(
                ExecutionContextData.HttpStatusCode,
                HttpStatusCode.Unauthorized);

            if (await _stream.RecordExpiryAsync(_callerToken) is not { } failure)
            {
                return false;
            }

            _stream._reportFailure(failure);
            Current = ErrorHelper.SubscriptionFaulted(failure, _stream._errorHandler);
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            _linked.Dispose();
            _expired.Dispose();
        }
    }
}
