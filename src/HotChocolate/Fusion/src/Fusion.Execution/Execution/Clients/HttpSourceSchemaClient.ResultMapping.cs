using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed partial class HttpSourceSchemaClient
{
    private static ReadOnlySpan<byte> VariableIndex => "variableIndex"u8;
    private static ReadOnlySpan<byte> RequestIndex => "requestIndex"u8;

    /// <summary>
    /// Creates the result of a response to a request with at most one variable value set.
    /// </summary>
    private static SourceSchemaResult CreateSingleResult(
        ImmutableArray<VariableValues> variables,
        SourceResultDocument document)
    {
        if (variables.Length == 0)
        {
            return new SourceSchemaResult(CompactPath.Root, document);
        }

        ref readonly var variable = ref variables.ItemRef(0);
        return new SourceSchemaResult(variable.Path, document, additionalPaths: variable.AdditionalPaths);
    }

    private static bool TryCreateResult(
        in SourceSchemaClientRequest request,
        int variableIndex,
        SourceResultDocument document,
        [NotNullWhen(true)] out SourceSchemaResult? result)
    {
        if (!TryGetResultPath(in request, variableIndex, out var path, out var additionalPaths))
        {
            result = null;
            return false;
        }

        result = additionalPaths.IsDefaultOrEmpty
            ? new SourceSchemaResult(path, document)
            : new SourceSchemaResult(path, document, additionalPaths: additionalPaths);
        return true;
    }

    private static int ResolveVariableIndex(
        in SourceSchemaClientRequest request,
        SourceResultDocument result)
    {
        var variableCount = request.Variables.Length;

        if (variableCount <= 1)
        {
            return 0;
        }

        if (!result.Root.TryGetProperty(VariableIndex, out var variableIndexElement))
        {
            return -1;
        }

        var variableIndex = variableIndexElement.GetInt32();

        if ((uint)variableIndex < (uint)variableCount)
        {
            return variableIndex;
        }

        throw ThrowHelper.VariableIndexOutOfRange(variableIndex);
    }

    /// <summary>
    /// Resolves the position of an operation in a sent batch to the request it belongs to,
    /// and to the variable value set it carries, or <c>-1</c> when it is a variable batch
    /// that carries every variable value set of the request.
    /// </summary>
    /// <param name="body">
    /// The body that was sent: an operation batch that holds, in request order, either one
    /// operation or variable batch per request, or one operation for each variable value set
    /// of each request; or, for a batch of one request, that request's own operation or
    /// variable batch.
    /// </param>
    /// <param name="operationIndex">The position of the operation in the batch.</param>
    /// <param name="requests">The requests of the batch.</param>
    /// <param name="requestIndex">The index of the request the operation belongs to.</param>
    /// <param name="variableIndex">
    /// The index of the variable value set the operation carries.
    /// </param>
    private static void ResolveOperation(
        IRequestBody body,
        int operationIndex,
        ImmutableArray<SourceSchemaClientRequest> requests,
        out int requestIndex,
        out int variableIndex)
    {
        if (body is not OperationBatchRequest batch)
        {
            if (operationIndex == 0)
            {
                requestIndex = 0;
                variableIndex = body is VariableBatchRequest ? -1 : 0;
                return;
            }

            throw ThrowHelper.RequestIndexOutOfRange(operationIndex);
        }

        var operations = batch.Requests;

        if ((uint)operationIndex < (uint)operations.Length)
        {
            // Every request has exactly one operation when the counts match.
            if (operations.Length == requests.Length)
            {
                requestIndex = operationIndex;
                variableIndex = operations[operationIndex] is VariableBatchRequest ? -1 : 0;
                return;
            }

            // Otherwise every request has one operation for each of its variable value sets.
            var position = 0;

            for (var i = 0; i < requests.Length; i++)
            {
                var operationCount = Math.Max(1, requests.ItemRef(i).Variables.Length);

                if (operationIndex < position + operationCount)
                {
                    requestIndex = i;
                    variableIndex = operationIndex - position;
                    return;
                }

                position += operationCount;
            }
        }

        throw ThrowHelper.RequestIndexOutOfRange(operationIndex);
    }

    private static bool TryGetResultPath(
        in SourceSchemaClientRequest request,
        int variableIndex,
        out CompactPath path,
        out CompactPathSegment additionalPaths)
    {
        var variables = request.Variables;

        if (variables.Length == 0)
        {
            path = CompactPath.Root;
            additionalPaths = default;
            return true;
        }

        if ((uint)variableIndex >= (uint)variables.Length)
        {
            path = CompactPath.Root;
            additionalPaths = default;
            return false;
        }

        ref readonly var variable = ref variables.ItemRef(variableIndex);
        path = variable.Path;
        additionalPaths = variable.AdditionalPaths;
        return true;
    }

    /// <summary>
    /// Maps the documents of a response to a request with many variable value sets onto those
    /// variable value sets.
    /// </summary>
    private struct VariableBatchResultMapper
    {
        private readonly ImmutableArray<VariableValues> _variables;
        private readonly bool _isVariableBatch;
        private bool? _hasRequestIndex;
        private int _nextIndex;
        private SourceSchemaResult? _errorResult;

        /// <summary>
        /// Initializes a mapper for the given variable value sets.
        /// </summary>
        /// <param name="variables">The variable value sets of the request.</param>
        /// <param name="body">
        /// The body that was sent: a variable batch, whose documents name their variable value
        /// set with a <c>variableIndex</c>, or an operation batch with one operation per variable
        /// value set, whose documents name it with a <c>requestIndex</c>.
        /// </param>
        /// <param name="alwaysSendsRequestIndex">
        /// <c>true</c> when the source schema names every document of an operation batch with a
        /// <c>requestIndex</c>; <c>false</c> when its documents may carry none and arrive in the
        /// order of the variable value sets.
        /// </param>
        public VariableBatchResultMapper(
            ImmutableArray<VariableValues> variables,
            IRequestBody body,
            bool alwaysSendsRequestIndex)
        {
            _variables = variables;
            _isVariableBatch = body is VariableBatchRequest;
            _hasRequestIndex = alwaysSendsRequestIndex ? true : null;
        }

        /// <summary>
        /// Gets the number of error results to produce once reading stopped at a protocol error.
        /// </summary>
        public readonly int ErrorResultCount => _errorResult is null ? 0 : _variables.Length;

        /// <summary>
        /// Maps a document onto its variable value set. Returns <c>false</c> when the document
        /// breaks the batch protocol, in which case reading must stop and the error results follow.
        /// </summary>
        public bool TryMap(SourceResultDocument document, [NotNullWhen(true)] out SourceSchemaResult? result)
        {
            if (!TryGetIndex(document, out var index))
            {
                // if we do not get the index the batch calls for we have a protocol error
                // and must terminate the request.
                SetErrorResult(document, out result);
                return false;
            }

            if ((uint)index >= (uint)_variables.Length)
            {
                // if the index is larger than the amount of variable sets we have
                // we also have a protocol issue and must terminate the request.
                SetErrorResult(document, out result);
                return false;
            }

            ref readonly var variable = ref _variables.ItemRef(index);
            result = new SourceSchemaResult(variable.Path, document, additionalPaths: variable.AdditionalPaths);
            return true;
        }

        /// <summary>
        /// Creates the error result for the variable value set at <paramref name="index"/>.
        /// </summary>
        public readonly SourceSchemaResult CreateErrorResult(int index)
        {
            ref readonly var variable = ref _variables.ItemRef(index);
            return _errorResult!.WithPath(variable.Path, variable.AdditionalPaths);
        }

        private bool TryGetIndex(SourceResultDocument document, out int index)
        {
            if (_isVariableBatch)
            {
                return TryReadIndex(document, VariableIndex, out index);
            }

            _hasRequestIndex ??= document.Root.TryGetProperty(RequestIndex, out _);

            if (_hasRequestIndex.Value)
            {
                return TryReadIndex(document, RequestIndex, out index);
            }

            index = _nextIndex++;
            return true;
        }

        private static bool TryReadIndex(
            SourceResultDocument result,
            ReadOnlySpan<byte> propertyName,
            out int index)
        {
            if (result.Root.TryGetProperty(propertyName, out var indexElement)
                && indexElement.ValueKind is JsonValueKind.Number)
            {
                index = indexElement.GetInt32();
                return true;
            }

            index = -1;
            return false;
        }

        private void SetErrorResult(SourceResultDocument document, out SourceSchemaResult? result)
        {
            _errorResult = new SourceSchemaResult(_variables.ItemRef(0).Path, document);
            result = null;
        }
    }

    /// <summary>
    /// Maps the documents of an operation batch response onto the requests of the batch.
    /// </summary>
    private struct BatchResultMapper
    {
        private readonly ImmutableArray<SourceSchemaClientRequest> _requests;
        private readonly IRequestBody _body;
        private readonly bool _alwaysSendsRequestIndex;
        private bool? _hasRequestIndex;
        private int _currentRequestIndex;
        private int _currentVariableIndex;
        private SourceResultDocument? _document;
        private BatchResultKind _kind;
        private int _requestIndex;
        private int _variableIndex;
        private int _position;

        /// <summary>
        /// Initializes a mapper for the requests of a batch.
        /// </summary>
        /// <param name="requests">The requests of the batch.</param>
        /// <param name="body">
        /// The body that was sent: an operation batch that holds, in request order, either one
        /// operation or variable batch per request, or one operation for each variable value set
        /// of each request; or, for a batch of one request, that request's own operation or
        /// variable batch.
        /// </param>
        /// <param name="alwaysSendsRequestIndex">
        /// <c>true</c> when the source schema names every document of an operation batch with a
        /// <c>requestIndex</c>, so that a document without one answers the whole batch;
        /// <c>false</c> when its documents may carry none and arrive in the order of the
        /// operations.
        /// </param>
        public BatchResultMapper(
            ImmutableArray<SourceSchemaClientRequest> requests,
            IRequestBody body,
            bool alwaysSendsRequestIndex)
        {
            _requests = requests;
            _body = body;
            _alwaysSendsRequestIndex = alwaysSendsRequestIndex;
        }

        /// <summary>
        /// Sets the response document whose results <see cref="TryGetNext"/> returns.
        /// </summary>
        public void Begin(SourceResultDocument document)
        {
            _document = document;
            _position = 0;

            if (!_alwaysSendsRequestIndex)
            {
                // Check if the first result has a requestIndex.
                // If it does we can assume all others will have one as well and we know we need to check for it.
                // If it doesn't we can assume there won't be one and we can just simple counters.
                _hasRequestIndex ??= document.Root.TryGetProperty(RequestIndex, out _);

                if (_hasRequestIndex.Value)
                {
                    if (!document.Root.TryGetProperty(RequestIndex, out var requestIndex))
                    {
                        throw ThrowHelper.MissingBatchRequestIndex();
                    }

                    BeginOperation(document, requestIndex.GetInt32());
                    return;
                }

                _requestIndex = _currentRequestIndex;
                _variableIndex = _currentVariableIndex;

                if (++_currentVariableIndex
                    >= Math.Max(1, _requests.ItemRef(_currentRequestIndex).Variables.Length))
                {
                    _currentRequestIndex++;
                    _currentVariableIndex = 0;
                }

                _kind = BatchResultKind.Single;
                return;
            }

            if (document.Root.TryGetProperty(RequestIndex, out var operationIndex))
            {
                BeginOperation(document, operationIndex.GetInt32());
                return;
            }

            if (_requests.Length > 1)
            {
                _kind = BatchResultKind.EveryRequest;
                return;
            }

            _requestIndex = 0;
            BeginVariableBatch(document);
        }

        /// <summary>
        /// Maps the document onto the operation at <paramref name="operationIndex"/> of the batch.
        /// </summary>
        private void BeginOperation(SourceResultDocument document, int operationIndex)
        {
            ResolveOperation(
                _body,
                operationIndex,
                _requests,
                out _requestIndex,
                out var variableIndex);

            if (variableIndex == -1)
            {
                BeginVariableBatch(document);
                return;
            }

            _variableIndex = variableIndex;
            _kind = BatchResultKind.Single;
        }

        /// <summary>
        /// Maps the document onto the variable value set that it names with a <c>variableIndex</c>,
        /// or onto every variable value set of the request when it names none.
        /// </summary>
        private void BeginVariableBatch(SourceResultDocument document)
        {
            ref readonly var request = ref _requests.ItemRef(_requestIndex);
            _variableIndex = ResolveVariableIndex(in request, document);

            if (_variableIndex == -1)
            {
                _kind = BatchResultKind.EveryVariableValueSet;
                return;
            }

            if (!TryGetResultPath(in request, _variableIndex, out _, out _))
            {
                document.Dispose();
                throw ThrowHelper.InvalidVariableIndex(_variableIndex, request.Node.Id);
            }

            _kind = BatchResultKind.Single;
        }

        /// <summary>
        /// Takes the next result that the current document maps to.
        /// </summary>
        /// <param name="requestIndex">The index of the request the result belongs to.</param>
        /// <param name="result">The result.</param>
        /// <returns><c>true</c> when a result was taken; <c>false</c> when the document is fully mapped.</returns>
        public bool TryGetNext(out int requestIndex, [NotNullWhen(true)] out SourceSchemaResult? result)
        {
            switch (_kind)
            {
                case BatchResultKind.Single:
                    _kind = BatchResultKind.None;

                    if (TryCreateResult(in _requests.ItemRef(_requestIndex), _variableIndex, _document!, out result))
                    {
                        requestIndex = _requestIndex;
                        return true;
                    }

                    break;

                case BatchResultKind.EveryRequest:
                    while (_position < _requests.Length)
                    {
                        var index = _position++;

                        if (TryCreateResult(in _requests.ItemRef(index), variableIndex: 0, _document!, out result))
                        {
                            requestIndex = index;
                            return true;
                        }
                    }

                    break;

                case BatchResultKind.EveryVariableValueSet:
                    ref readonly var request = ref _requests.ItemRef(_requestIndex);

                    while (_position < request.Variables.Length)
                    {
                        if (TryCreateResult(in request, _position++, _document!, out result))
                        {
                            requestIndex = _requestIndex;
                            return true;
                        }
                    }

                    break;
            }

            _kind = BatchResultKind.None;
            _document = null;
            requestIndex = -1;
            result = null;
            return false;
        }
    }

    private enum BatchResultKind
    {
        /// <summary>
        /// The document maps to no further result.
        /// </summary>
        None,

        /// <summary>
        /// The document maps to the result of one variable value set of one request.
        /// </summary>
        Single,

        /// <summary>
        /// The document carries no request index and maps to every request.
        /// </summary>
        EveryRequest,

        /// <summary>
        /// The document carries no variable index and maps to every variable value set of its request.
        /// </summary>
        EveryVariableValueSet
    }
}
