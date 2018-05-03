using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{







    /*
    internal class QueryOperationExecuter
    {
        private readonly ArgumentResolver _argumentResolver = new ArgumentResolver();
        private readonly Func<Type, object> _resolveService;
        private readonly ISchema _schema;
        private readonly DocumentNode _queryDocument;
        private readonly OperationDefinitionNode _operation;
        private readonly FragmentCollection _fragments;

        public QueryOperationExecuter(
            ISchema schema,
            DocumentNode queryDocument,
            OperationDefinitionNode operation,
            FragmentCollection fragments,
            Func<Type, object> resolveService)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (fragments == null)
            {
                throw new ArgumentNullException(nameof(fragments));
            }

            if (resolveService == null)
            {
                throw new ArgumentNullException(nameof(resolveService));
            }

            _schema = schema;
            _queryDocument = queryDocument;
            _operation = operation;
            _fragments = fragments;
        }

        public async Task<QueryResult> ExecuteAsync(
            VariableCollection variables,
            object queryObject,
            CancellationToken cancellationToken)
        {
            ObjectType queryType = _schema.Query;
            FieldResolver fieldResolver = new FieldResolver(variables, _fragments);
            IReadOnlyCollection<FieldSelection> fieldSelections =
                fieldResolver.CollectFields(queryType, _operation.SelectionSet, error => { });

            List<FieldExecutionContext> fieldTasks = new List<FieldExecutionContext>();
            foreach (FieldSelection fieldSelection in fieldSelections)
            {
                ResolverContext resolverContext = CreateRootResolverContext(
                    queryObject, fieldSelection, variables);
                Task<object> task = fieldSelection.Field.Resolver(
                    resolverContext, cancellationToken);
                fieldTasks.Add(new FieldExecutionContext(fieldSelection, resolverContext, task));
            }

            // await Task.WhenAll(tasks.Values);


            throw new Exception();
        }

        public async Task<QueryResult> ExecuteAsync2(
            VariableCollection variables,
            object queryObject,
            CancellationToken cancellationToken)
        {
            List<FieldResolverSession> currentBatch = new List<FieldResolverSession>();
            List<FieldResolverSession> nextBatch = new List<FieldResolverSession>();

            while (currentBatch.Any())
            {
                foreach (FieldResolverSession session in currentBatch)
                {
                    session.Start(cancellationToken);
                }

                await WaitForCompletion(currentBatch);

                foreach (FieldResolverSession session in currentBatch)
                {
                    await CompleteFieldValue(session, nextBatch);
                }

                currentBatch = nextBatch;
                nextBatch = new List<FieldResolverSession>();
            }


            // await Task.WhenAll(tasks.Values);


            throw new Exception();
        }

        private async Task CompleteFieldValue(FieldResolverSession fieldResolverSession, List<FieldResolverSession> nextBatch)
        {
            try
            {
                object result = await fieldExecutionContext.Task;
                IOutputType type = fieldExecutionContext.FieldSelection.Field.Type;

                if (type.IsNonNullType() && result == null)
                {
                    // throw field error
                }



                if (type.IsListType())
                {
                    if (type.ElementType() is ScalarType st)
                    {

                    }
                }

                if (type is ScalarType scalarType)
                {
                    fieldExecutionContext.SetResult(scalarType.Serialize(result));
                }


            }
            catch
            {

            }

        }

        private void X(FieldExecutionContext fieldExecutionContext, object result, IOutputType type)
        {
            bool isNonNullType = type.IsNonNullElementType();
            if (type.ElementType() is ScalarType scalarType)
            {
                Array array = (Array)result;
                object[] serializedResult = new object[array.Length];

                for (int i = 0; i < array.Length; i++)
                {
                    object element = array.GetValue(i);
                    if (isNonNullType && element == null)
                    {
                        // field error
                    }
                    serializedResult[i] = scalarType.Serialize(element);
                }

                fieldExecutionContext.SetResult(serializedResult);
            }
            else
            {
                Array array = (Array)result;
                object[] serializedResult = new object[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    object element = array.GetValue(i);
                    if (isNonNullType && element == null)
                    {
                        // field error
                    }
                    serializedResult[i] = new Dictionary<string, object>();
                }
            }
        }


        private ResolverContext CreateRootResolverContext(
            object objectValue,
            FieldSelection fieldSelection,
            VariableCollection variables)
        {
            SchemaContextInfo schemaContext = new SchemaContextInfo
            (
                _schema,
                _schema.Query,
                fieldSelection.Field
            );

            QueryContextInfo queryContext = new QueryContextInfo
            (
                _queryDocument,
                _operation,
                fieldSelection.Node,
                _argumentResolver.CoerceArgumentValues(
                    _schema.Query, fieldSelection, variables)
            );

            return new ResolverContext(schemaContext, queryContext,
                ImmutableStack<object>.Empty.Push(objectValue), _resolveService);
        }

        private ResolverContext CreateResolverContext(
            ResolverContext parentContext,
            ObjectType objectType,
            object objectValue,
            FieldSelection fieldSelection,
            VariableCollection variables)
        {
            return ResolverContext.Create(
                parentContext,
                objectType,
                objectValue,
                fieldSelection.Field,
                fieldSelection.Node,
                _argumentResolver.CoerceArgumentValues(
                    _schema.Query, fieldSelection, variables)
            );
        }






    }

*/

}
