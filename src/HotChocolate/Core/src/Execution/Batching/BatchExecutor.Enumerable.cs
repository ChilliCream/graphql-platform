using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Batching
{
    internal partial class BatchExecutor
    {
        private class BatchExecutorEnumerable : IAsyncEnumerable<IQueryResult>
        {
            private readonly IEnumerable<IQueryRequest> _requestBatch;
            private readonly IRequestExecutor _requestExecutor;
            private readonly IErrorHandler _errorHandler;
            private readonly ITypeConverter _typeConverter;
            private readonly ConcurrentBag<ExportedVariable> _exportedVariables =
                new ConcurrentBag<ExportedVariable>();
            private readonly CollectVariablesVisitor _visitor;
            private readonly CollectVariablesVisitationMap _visitationMap =
                new CollectVariablesVisitationMap();
            private DocumentNode? _previous;
            private Dictionary<string, FragmentDefinitionNode>? _fragments;
            private bool _allowParallelExecution;
            private readonly List<IBatchDispatcher> _batchDispatchers;
            private static readonly List<IBatchDispatcher> EmptyBatchDispatchers = new();

            public BatchExecutorEnumerable(
                IEnumerable<IQueryRequest> requestBatch,
                IRequestExecutor requestExecutor,
                IErrorHandler errorHandler,
                ITypeConverter typeConverter,
                bool allowParallelExecution = false)
            {
                _requestBatch = requestBatch ??
                    throw new ArgumentNullException(nameof(requestBatch));
                _requestExecutor = requestExecutor ??
                    throw new ArgumentNullException(nameof(requestExecutor));
                _errorHandler = errorHandler ??
                    throw new ArgumentNullException(nameof(errorHandler));
                _typeConverter = typeConverter ??
                    throw new ArgumentNullException(nameof(typeConverter));
                _visitor = new CollectVariablesVisitor(requestExecutor.Schema);
                _allowParallelExecution = allowParallelExecution;
                if (_allowParallelExecution)
                {
                    // note: if Services isn't overwritten, a new IBatchDispatcher will
                    //       be allocated for each request, so suspending/resuming will
                    //       never have effect so we can ignore it here as well
                    _batchDispatchers = requestBatch
                        .Where(x => x.Services is not null)
                        .Select(x => x.Services.GetRequiredService<IBatchDispatcher>())
                        .Distinct()
                        .ToList();
                }
                else
                {
                    _batchDispatchers = EmptyBatchDispatchers;
                }

            }

            public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                _batchDispatchers.ForEach(x => x.Suspend());
                try
                {
                    var pendingTasks = new List<Task<IQueryResult>>();
                    IEnumerator<IQueryRequest> requestIterator = _requestBatch.GetEnumerator();
                    bool hasRemaining = requestIterator.MoveNext();
                    while (hasRemaining)
                    {
                        var request = (IReadOnlyQueryRequest)requestIterator.Current;
                        NextResult next = ExecuteNextAsync(request, pendingTasks, cancellationToken);
                        pendingTasks.Add(next._task);
                        hasRemaining = requestIterator.MoveNext();
                        if (next._isBlocking || !_allowParallelExecution || !hasRemaining)
                        {
                            _batchDispatchers.ForEach(x => x.Resume());
                            try
                            {
                                bool keepRunning = true;
                                foreach (Task<IQueryResult> task in pendingTasks)
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
                                hasRemaining &= keepRunning;
                                pendingTasks.Clear();
                            }
                            finally
                            {
                                _batchDispatchers.ForEach(x => x.Suspend());
                            }
                        }
                    }
                }
                finally
                {
                    _batchDispatchers.ForEach(x => x.Resume());
                }
            }

            private NextResult ExecuteNextAsync(
                IReadOnlyQueryRequest request,
                List<Task<IQueryResult>> pendingTasks,
                CancellationToken cancellationToken)
            {
                try
                {
                    DocumentNode document = request.Query is QueryDocument d
                        ? d.Document
                        : Utf8GraphQLParser.Parse(request.Query!.AsSpan());

                    OperationDefinitionNode operation =
                        document.GetOperation(request.OperationName);

                    if (document != _previous)
                    {
                        _fragments = document.GetFragments();
                        _visitationMap.Initialize(_fragments);
                    }

                    int oldExportCount = _visitor.ExportCount;
                    operation.Accept(
                        _visitor,
                        _visitationMap,
                        n => VisitorAction.Continue);
                    // if there are exports in the operation, or the current operation is not a query
                    // the next operation cannot start until the current one is completed
                    bool operationIsBlocking = operation.Operation != OperationType.Query ||
                                               _visitor.ExportCount != oldExportCount;

                    _previous = document;
                    document = RewriteDocument(operation);
                    operation = (OperationDefinitionNode)document.Definitions[0];
                    IReadOnlyDictionary<string, object?>? variableValues =
                        MergeVariables(request.VariableValues, operation);

                    request = QueryRequestBuilder.From(request)
                        .SetQuery(document)
                        .SetVariableValues(variableValues)
                        .AddExportedVariables(_exportedVariables)
                        .SetQueryId(null) // TODO ... should we create a name here?
                        .SetQueryHash(null)
                        .Create();

                    Func<Task<IExecutionResult>> queryResult = async () =>
                    {
                        // mutations should not start before all preceding queries have completed
                        if (operation.Operation != OperationType.Query)
                        {
                            // take shallow copy so we ignore any tasks added after this
                            await Task.WhenAll(pendingTasks.ToList()).ConfigureAwait(false);
                        }
                        return await _requestExecutor.ExecuteAsync(request, cancellationToken);
                    };
                    return new NextResult(queryResult(), operationIsBlocking);
                }
                catch (GraphQLException ex)
                {
                    return new NextResult(QueryResultBuilder.CreateError(ex.Errors));
                }
                catch (Exception ex)
                {
                    return new NextResult(QueryResultBuilder.CreateError(
                        _errorHandler.Handle(
                            _errorHandler.CreateUnexpectedError(ex).Build())));
                }
            }

            private class NextResult
            {
                public readonly Task<IQueryResult> _task;
                /// <summary>
                /// if \c true, no other calls to ExecuteNextAsync should be made before _task is completed.
                /// if \c false, it is safe to schedule other ExecuteNextAsync calls before completing.
                /// </summary>
                public readonly bool _isBlocking;

                public NextResult(IQueryResult result)
                {
                    _task = Task.FromResult(result);
                    _isBlocking = result.Data is null;
                }

                public NextResult(Task<IExecutionResult> result, bool isBlocking)
                {
                    Func<Task<IQueryResult>> cast = async () => {
                        return (IQueryResult) await result.ConfigureAwait(false);
                    };
                    _task = cast();
                    _isBlocking = isBlocking;
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

                foreach (VariableDefinitionNode variableDefinition in
                    operation.VariableDefinitions)
                {
                    string variableName = variableDefinition.Variable.Name.Value;

                    if (!exported[variableName].Any())
                    {
                        if (variables != null
                            && variables.TryGetValue(variableName, out object? value))
                        {
                            merged[variableName] = value;
                        }
                    }
                    else if (variableDefinition.Type.IsListType())
                    {
                        var list = new List<object?>();

                        if (variables != null
                            && variables.TryGetValue(variableName, out object? value))
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

            private object Serialize(ExportedVariable exported, ITypeNode type)
            {
                if (_requestExecutor.Schema.TryGetType(
                    type.NamedType().Name.Value,
                    out INamedInputType inputType)
                    && _typeConverter.TryConvert(
                        typeof(object),
                        inputType.RuntimeType,
                        exported.Value,
                        out object? converted))
                {
                    return inputType.Serialize(converted);
                }

                throw BatchExecutor_CannotSerializeVariable(exported.Name);
            }

            private void SerializeListValue(
                ExportedVariable exported,
                ITypeNode type,
                ICollection<object?> list)
            {
                if (_requestExecutor.Schema.TryGetType(
                    type.NamedType().Name.Value,
                    out INamedInputType inputType))
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
                if (exported.Type.IsListType()
                    && exported.Value is IEnumerable l)
                {
                    foreach (var o in l)
                    {
                        if (_typeConverter.TryConvert(
                            typeof(object),
                            inputType.RuntimeType,
                            o,
                            out object? converted))
                        {
                            list.Add(inputType.Serialize(converted));
                        }
                        else
                        {
                            throw BatchExecutor_CannotSerializeVariable(exported.Name);
                        }
                    }
                }
                else
                {
                    if (_typeConverter.TryConvert(
                        typeof(object),
                        inputType.RuntimeType,
                        exported.Value,
                        out var converted))
                    {
                        list.Add(inputType.Serialize(converted));
                    }
                    else
                    {
                        throw BatchExecutor_CannotSerializeVariable(exported.Name);
                    }
                }
            }
        }
    }
}
