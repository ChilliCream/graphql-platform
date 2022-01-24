using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Batching;

internal partial class BatchExecutor
{
    private class BatchExecutorEnumerable : IAsyncEnumerable<IQueryResult>
    {
        private readonly IEnumerable<IQueryRequest> _requestBatch;
        private readonly IRequestExecutor _requestExecutor;
        private readonly IErrorHandler _errorHandler;
        private readonly ITypeConverter _typeConverter;
        private readonly InputFormatter _inputFormatter;
        private readonly IExecutionDiagnosticEvents _executionDiagnosticEvents;
        private readonly int _maxConcurrentQueries;
        private readonly ConcurrentBag<ExportedVariable> _exportedVariables = new();
        private readonly CollectVariablesVisitor _visitor;
        private readonly CollectVariablesVisitationMap _visitationMap = new();
        private DocumentNode? _previous;
        private Dictionary<string, FragmentDefinitionNode>? _fragments;

        public BatchExecutorEnumerable(
            IEnumerable<IQueryRequest> requestBatch,
            IRequestExecutor requestExecutor,
            IErrorHandler errorHandler,
            ITypeConverter typeConverter,
            InputFormatter inputFormatter,
            IExecutionDiagnosticEvents executionDiagnosticEvents,
            int maxConcurrentQueries)
        {
            _requestBatch = requestBatch ??
                throw new ArgumentNullException(nameof(requestBatch));
            _requestExecutor = requestExecutor ??
                throw new ArgumentNullException(nameof(requestExecutor));
            _errorHandler = errorHandler ??
                throw new ArgumentNullException(nameof(errorHandler));
            _typeConverter = typeConverter ??
                throw new ArgumentNullException(nameof(typeConverter));
            _inputFormatter = inputFormatter ??
                throw new ArgumentNullException(nameof(inputFormatter));
            _executionDiagnosticEvents = executionDiagnosticEvents ??
                throw new ArgumentNullException(nameof(executionDiagnosticEvents));
            _maxConcurrentQueries = maxConcurrentQueries;
            if (_maxConcurrentQueries <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentQueries));
            }
            _visitor = new CollectVariablesVisitor(requestExecutor.Schema);
        }

        public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            foreach (var groupedItems in GroupedItems())
            {
                using(var diagnostics = _executionDiagnosticEvents.ExecuteBatchedQueryGroup())
                {
                    var tasks = groupedItems.Select(x => x.Execute(cancellationToken)).ToList();
                    bool keepRunning = true;

                    foreach (var task in tasks)
                    {
                        IQueryResult result = await task.ConfigureAwait(false);
                        if (keepRunning)
                        {
                            yield return result;
                        }
                        if (result.Data is null)
                        {
                            keepRunning = false;
                        }
                    }

                    if (!keepRunning)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Convert the queries from _requestBatch into groups of tasks that can be executed in parallel.
        /// </summary>
        private IEnumerable<IReadOnlyList<WorkItem>> GroupedItems()
        {
            var grouped = new List<WorkItem>();
            foreach (IQueryRequest request in _requestBatch)
            {
                var item = new WorkItem((IReadOnlyQueryRequest)request, this);
                if (grouped.Count > 0 && item.IsBlockedByPending)
                {
                    yield return grouped;
                    grouped.Clear();
                }
                grouped.Add(item);
                if (grouped.Count >= _maxConcurrentQueries || item.IsBlocking)
                {
                    yield return grouped;
                    grouped.Clear();
                }
              }
            if (grouped.Count > 0)
            {
                yield return grouped;
            }
        }

        private sealed class WorkItem
        {
            private readonly BatchExecutorEnumerable _parent;
            private readonly IReadOnlyQueryRequest _request;
            private readonly OperationDefinitionNode _operation;
            private readonly int _exportCount;
            private readonly IQueryResult _error = default!;

            public WorkItem(IReadOnlyQueryRequest request, BatchExecutorEnumerable parent)
            {
                _parent = parent;
                try
                {
                    DocumentNode document = request.Query is QueryDocument d
                        ? d.Document
                        : Utf8GraphQLParser.Parse(request.Query!.AsSpan());

                    _operation = document.GetOperation(request.OperationName);

                    if (document != _parent._previous)
                    {
                        _parent._fragments = document.GetFragments();
                        _parent._visitationMap.Initialize(_parent._fragments);
                    }

                    int oldExportCount = _parent._visitor.ExportCount;
                    _operation.Accept(
                        _parent._visitor,
                        _parent._visitationMap,
                        _ => VisitorAction.Continue);
                    _exportCount = _parent._visitor.ExportCount - oldExportCount;

                    _parent._previous = document;
                    document = _parent.RewriteDocument(_operation);
                    _operation = (OperationDefinitionNode)document.Definitions[0];
                    IReadOnlyDictionary<string, object?>? variableValues =
                        _parent.MergeVariables(request.VariableValues, _operation);

                    _request = QueryRequestBuilder.From(request)
                        .SetQuery(document)
                        .SetVariableValues(variableValues)
                        .AddExportedVariables(_parent._exportedVariables)
                        .SetQueryId(null) // TODO ... should we create a name here?
                        .SetQueryHash(null)
                        .Create();
                }
                catch (GraphQLException ex)
                {
                    _error = QueryResultBuilder.CreateError(ex.Errors);
                }
                catch (Exception ex)
                {
                    _error = QueryResultBuilder.CreateError(
                        _parent._errorHandler.Handle(
                            _parent._errorHandler.CreateUnexpectedError(ex).Build()));
                }
            }

            /// <summary>If true, all pending work items should be handled before this one should start</summary>
            public bool IsBlockedByPending => _operation?.Operation != OperationType.Query;

            /// <summary>If true, no new work items should start before this one completes</summary>
            public bool IsBlocking => _error is not null || _operation.Operation != OperationType.Query || _exportCount > 0;

            public async Task<IQueryResult> Execute(CancellationToken cancellationToken)
            {
                if (_error is null)
                {
                    try
                    {
                        return (IQueryResult) await _parent._requestExecutor.ExecuteAsync(_request, cancellationToken).ConfigureAwait(false);
                    }
                    catch (GraphQLException ex)
                    {
                        return QueryResultBuilder.CreateError(ex.Errors);
                    }
                    catch (Exception ex)
                    {
                        return QueryResultBuilder.CreateError(
                            _parent._errorHandler.Handle(
                                _parent._errorHandler.CreateUnexpectedError(ex).Build()));
                    }
                }
                else
                {
                    return _error;
                }

            }
        }

        private DocumentNode RewriteDocument(
            OperationDefinitionNode operation)
        {
            var definitions = new List<IDefinitionNode>();

            var variables = operation.VariableDefinitions.ToList();
            variables.AddRange(_visitor.VariableDeclarations);
            operation = operation.WithVariableDefinitions(variables);
            definitions.Add(operation);

            foreach (var fragmentName in _visitor.TouchedFragments)
            {
                definitions.Add(_fragments![fragmentName]);
            }

            return new DocumentNode(definitions);
        }

        private IReadOnlyDictionary<string, object?>? MergeVariables(
            IReadOnlyDictionary<string, object?>? variables,
            OperationDefinitionNode operation)
        {
            if (_exportedVariables.Count == 0)
            {
                return variables;
            }

            ILookup<string, ExportedVariable> exported =
                _exportedVariables.ToLookup(t => t.Name);
            var merged = new Dictionary<string, object?>();

            foreach (VariableDefinitionNode variableDefinition in operation.VariableDefinitions)
            {
                string variableName = variableDefinition.Variable.Name.Value;

                if (!exported[variableName].Any())
                {
                    if (variables != null
                        && variables.TryGetValue(variableName, out var value))
                    {
                        merged[variableName] = value;
                    }
                }
                else if (variableDefinition.Type.IsListType())
                {
                    var list = new List<object?>();

                    if (variables != null
                        && variables.TryGetValue(variableName, out var value))
                    {
                        if (value is IReadOnlyCollection<object?> l)
                        {
                            list.AddRange(l);
                        }
                        else
                        {
                            list.Add(value);
                        }
                    }

                    foreach (ExportedVariable variable in
                        exported[variableName])
                    {
                        SerializeListValue(
                            variable,
                            variableDefinition.Type,
                            list);
                    }

                    merged[variableName] = list;
                }
                else
                {
                    if (variables != null
                        && variables.TryGetValue(variableName, out var value))
                    {
                        merged[variableName] = value;
                    }
                    else
                    {
                        merged[variableName] = Serialize(
                            exported[variableName].First(),
                            variableDefinition.Type);
                    }
                }

            }

            return merged;
        }

        private object? Serialize(ExportedVariable exported, ITypeNode type)
        {
            if (_requestExecutor.Schema.TryGetType<INamedInputType>(
                type.NamedType().Name.Value,
                out INamedInputType? inputType)
                && _typeConverter.TryConvert(
                    inputType!.RuntimeType,
                    exported.Value,
                    out var converted))
            {
                return _inputFormatter.FormatResult(converted, inputType, Path.Root);
            }

            throw BatchExecutor_CannotSerializeVariable(exported.Name);
        }

        private void SerializeListValue(
            ExportedVariable exported,
            ITypeNode type,
            ICollection<object?> list)
        {
            if (_requestExecutor.Schema.TryGetType<INamedInputType>(
                type.NamedType().Name.Value,
                out INamedInputType? inputType))
            {
                SerializeListValue(exported, inputType, list);
            }
            else
            {
                throw BatchExecutor_CannotSerializeVariable(exported.Name);
            }
        }

        private void SerializeListValue(
            ExportedVariable exported,
            INamedInputType inputType,
            ICollection<object?> list)
        {
            Type runtimeType = inputType.RuntimeType;

            if (exported.Type.IsListType()
                && exported.Value is IEnumerable l)
            {
                foreach (var o in l)
                {
                    if (_typeConverter.TryConvert(runtimeType, o, out var converted))
                    {
                        list.Add(_inputFormatter.FormatResult(converted, inputType, Path.Root));
                    }
                    else
                    {
                        throw BatchExecutor_CannotSerializeVariable(exported.Name);
                    }
                }
            }
            else
            {
                if (_typeConverter.TryConvert(runtimeType, exported.Value, out var converted))
                {
                    list.Add(_inputFormatter.FormatResult(converted, inputType, Path.Root));
                }
                else
                {
                    throw BatchExecutor_CannotSerializeVariable(exported.Name);
                }
            }
        }
    }
}
