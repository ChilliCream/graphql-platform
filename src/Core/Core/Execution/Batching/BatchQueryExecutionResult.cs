using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Batching
{
    public class BatchQueryExecutionResult
        : IBatchQueryExecutionResult
    {
        private static readonly IReadOnlyDictionary<string, object> _empty =
            new Dictionary<string, object>();

        private readonly IQueryExecutor _executor;
        private readonly IErrorHandler _errorHandler;
        private readonly ITypeConversion _typeConversion;
        private readonly IReadOnlyList<IReadOnlyQueryRequest> _batch;
        private readonly ConcurrentBag<ExportedVariable> _exportedVariables =
            new ConcurrentBag<ExportedVariable>();
        private readonly CollectVariablesVisitor _visitor;
        private readonly CollectVariablesVisitationMap _visitationMap =
            new CollectVariablesVisitationMap();

        private DocumentNode _previous;
        private Dictionary<string, FragmentDefinitionNode> _fragments;
        private int _index;

        public BatchQueryExecutionResult(
            IQueryExecutor executor,
            IErrorHandler errorHandler,
            ITypeConversion typeConversion,
            IReadOnlyList<IReadOnlyQueryRequest> batch)
        {
            _executor = executor
                ?? throw new ArgumentNullException(nameof(executor));
            _errorHandler = errorHandler
                ?? throw new ArgumentNullException(nameof(errorHandler));
            _typeConversion = typeConversion
                ?? throw new ArgumentNullException(nameof(typeConversion));
            _batch = batch
                ?? throw new ArgumentNullException(nameof(batch));

            _visitor = new CollectVariablesVisitor(executor.Schema);
        }

        public IReadOnlyCollection<IError> Errors => Array.Empty<IError>();

        public IReadOnlyDictionary<string, object> Extensions => _empty;

        public IReadOnlyDictionary<string, object> ContextData => _empty;

        public bool IsCompleted { get; private set; }

        public Task<IReadOnlyQueryResult> ReadAsync() =>
            ReadAsync(CancellationToken.None);

        public async Task<IReadOnlyQueryResult> ReadAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyQueryRequest request = _batch[_index++];

                DocumentNode document = request.Query is QueryDocument d
                    ? d.Document
                    : Utf8GraphQLParser.Parse(request.Query.ToSpan());

                OperationDefinitionNode operation =
                    document.GetOperation(request.OperationName);

                if (document != _previous)
                {
                    _fragments = document.GetFragments();
                    _visitationMap.Initialize(_fragments);
                }

                operation.Accept(
                    _visitor,
                    _visitationMap,
                    n => VisitorAction.Continue);

                _previous = document;
                document = RewriteDocument(operation);
                operation = (OperationDefinitionNode)document.Definitions[0];
                IReadOnlyDictionary<string, object> variableValues =
                    MergeVariables(request.VariableValues, operation);

                request = QueryRequestBuilder.From(request)
                    .SetQuery(document)
                    .SetVariableValues(variableValues)
                    .AddExportedVariables(_exportedVariables)
                    .SetQueryName(null) // TODO ... should we create a name here?
                    .SetQueryHash(null)
                    .Create();

                var result =
                    (IReadOnlyQueryResult)await _executor.ExecuteAsync(
                        request, cancellationToken)
                        .ConfigureAwait(false);
                IsCompleted = _index >= _batch.Count;
                return result;
            }
            catch (QueryException ex)
            {
                IsCompleted = true;
                return QueryResult.CreateError(ex.Errors);
            }
            catch (Exception ex)
            {
                IsCompleted = true;
                return QueryResult.CreateError(
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
                definitions.Add(_fragments[fragmentName]);
            }

            return new DocumentNode(definitions);
        }

        private IReadOnlyDictionary<string, object> MergeVariables(
            IReadOnlyDictionary<string, object> variables,
            OperationDefinitionNode operation)
        {
            if (_exportedVariables.Count == 0)
            {
                return variables;
            }

            ILookup<string, ExportedVariable> exported =
                    _exportedVariables.ToLookup(t => t.Name);
            var merged = new Dictionary<string, object>();

            foreach (VariableDefinitionNode variableDefinition in
                operation.VariableDefinitions)
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
                    var list = new List<object>();

                    if (variables != null
                        && variables.TryGetValue(variableName, out var value))
                    {
                        if (value is IReadOnlyCollection<object> l)
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
                            exported[variableName].FirstOrDefault(),
                            variableDefinition.Type);
                    }
                }

            }

            return merged;
        }

        private object Serialize(ExportedVariable exported, ITypeNode type)
        {
            if (_executor.Schema.TryGetType(
                type.NamedType().Name.Value,
                out INamedInputType inputType)
                && _typeConversion.TryConvert(
                    typeof(object),
                    inputType.ClrType,
                    exported.Value,
                    out var converted))
            {
                return inputType.Serialize(converted);
            }

            throw SerializationError();
        }

        private void SerializeListValue(
            ExportedVariable exported,
            ITypeNode type,
            ICollection<object> list)
        {
            if (_executor.Schema.TryGetType(
                type.NamedType().Name.Value,
                out INamedInputType inputType))
            {
                SerializeListValue(exported, inputType, list);
            }
            else
            {
                throw SerializationError();
            }
        }

        private void SerializeListValue(
            ExportedVariable exported,
            INamedInputType inputType,
            ICollection<object> list)
        {
            if (exported.Type.IsListType()
                && exported.Value is IEnumerable l)
            {
                foreach (var o in l)
                {
                    if (_typeConversion.TryConvert(
                        typeof(object),
                        inputType.ClrType,
                        o,
                        out var converted))
                    {
                        list.Add(inputType.Serialize(converted));
                    }
                    else
                    {
                        throw SerializationError();
                    }
                }
            }
            else
            {
                if (_typeConversion.TryConvert(
                    typeof(object),
                    inputType.ClrType,
                    exported.Value,
                    out var converted))
                {
                    list.Add(inputType.Serialize(converted));
                }
                else
                {
                    throw SerializationError();
                }
            }
        }

        private static QueryException SerializationError()
        {
            return new QueryException(
                ErrorBuilder.New()
                    .SetMessage(CoreResources.BatchQueryExec_CannotSerialize)
                    .SetCode(BatchingErrorCodes.CannotSerialize)
                    .Build());
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            IsCompleted = true;
        }
    }
}
