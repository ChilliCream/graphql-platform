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
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Batching;

internal partial class BatchExecutor
{
    private sealed class BatchExecutorEnumerable : IAsyncEnumerable<IQueryResult>
    {
        private readonly IReadOnlyList<IQueryRequest> _requestBatch;
        private readonly IRequestExecutor _requestExecutor;
        private readonly IErrorHandler _errorHandler;
        private readonly ITypeConverter _typeConverter;
        private readonly InputFormatter _inputFormatter;
        private readonly ConcurrentBag<ExportedVariable> _exportedVariables = [];
        private readonly CollectVariablesVisitor _visitor;
        private readonly CollectVariablesVisitationMap _visitationMap = new();
        private DocumentNode? _previous;
        private Dictionary<string, FragmentDefinitionNode>? _fragments;

        public BatchExecutorEnumerable(
            IReadOnlyList<IQueryRequest> requestBatch,
            IRequestExecutor requestExecutor,
            IErrorHandler errorHandler,
            ITypeConverter typeConverter,
            InputFormatter inputFormatter)
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
            _visitor = new CollectVariablesVisitor(requestExecutor.Schema);
        }

        public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            for (var i = 0; i < _requestBatch.Count; i++)
            {
                var result = await ExecuteNextAsync(
                    _requestBatch[i],
                    cancellationToken)
                    .ConfigureAwait(false);

                yield return result;

                if (result.Data is null)
                {
                    break;
                }
            }
        }

        private async Task<IQueryResult> ExecuteNextAsync(
            IQueryRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var document = request.Query is QueryDocument d
                    ? d.Document
                    : Utf8GraphQLParser.Parse(request.Query!.AsSpan());

                var operation =
                    document.GetOperation(request.OperationName);

                if (document != _previous)
                {
                    _fragments = document.GetFragments();
                    _visitationMap.Initialize(_fragments);
                }

                operation.Accept(
                    _visitor,
                    _visitationMap,
                    _ => VisitorAction.Continue);

                _previous = document;
                document = RewriteDocument(operation);
                operation = (OperationDefinitionNode)document.Definitions[0];
                var variableValues =
                    MergeVariables(request.VariableValues, operation);

                request = QueryRequestBuilder.From(request)
                    .SetQuery(document)
                    .SetVariableValues(variableValues)
                    .AddExportedVariables(_exportedVariables)
                    .SetQueryId(null)
                    .SetQueryHash(null)
                    .Create();

                return (IQueryResult)await _requestExecutor.ExecuteAsync(
                    request, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (GraphQLException ex)
            {
                return QueryResultBuilder.CreateError(ex.Errors);
            }
            catch (Exception ex)
            {
                return QueryResultBuilder.CreateError(
                    _errorHandler.Handle(
                        _errorHandler.CreateUnexpectedError(ex).Build()));
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

            var exported = _exportedVariables.ToLookup(t => t.Name);
            var merged = new Dictionary<string, object?>();

            foreach (var variableDefinition in operation.VariableDefinitions)
            {
                var variableName = variableDefinition.Variable.Name.Value;

                if (!exported[variableName].Any())
                {
                    if (variables != null && variables.TryGetValue(variableName, out var value))
                    {
                        merged[variableName] = value;
                    }
                }
                else if (variableDefinition.Type.IsListType())
                {
                    var list = new List<object?>();

                    if (variables != null && variables.TryGetValue(variableName, out var value))
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

                    foreach (var variable in exported[variableName])
                    {
                        SerializeListValue(variable, variableDefinition.Type, list);
                    }

                    merged[variableName] = list;
                }
                else
                {
                    if (variables != null && variables.TryGetValue(variableName, out var value))
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
            if (_requestExecutor.Schema.TryGetType<INamedInputType>(
                type.NamedType().Name.Value,
                out var inputType)
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
                out var inputType))
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
            var runtimeType = inputType.RuntimeType;

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
