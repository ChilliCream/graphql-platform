using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Fusion.Policies.Audit;

namespace HotChocolate.Fusion.Policies;

internal sealed class RecordingAuditProvider(AuthorizationTestClient client) : IAuditProvider
{
    private readonly ConcurrentQueue<RecordingAuditScope> _scopes = [];
    private int _trailCount;

    public int TrailCount => _trailCount;

    public Exception? CommitFailure { get; init; }

    public (int Call, Exception Failure)? RecordFailure { get; init; }

    public Exception? FailFailure { get; init; }

    public ImmutableArray<RecordingAuditScope> Scopes => [.. _scopes];

    public IAuditTrail CreateTrail(IServiceProvider requestServices)
        => new RecordingAuditTrail(
            Interlocked.Increment(ref _trailCount),
            this,
            () => client.Requests.Length);

    public void AddScope(RecordingAuditScope scope) => _scopes.Enqueue(scope);
}
