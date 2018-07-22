using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
        : IExecutionContext
    {
        private readonly List<IQueryError> _errors = new List<IQueryError>();
        private readonly FieldCollector _fieldCollector;

        public ExecutionContext(
            ISchema schema,
            DocumentNode queryDocument,
            OperationDefinitionNode operation,
            OperationRequest request,
            VariableCollection variables)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            Services = request.Services;
            DataLoaders = request.DataLoaders;
            QueryDocument = queryDocument
                ?? throw new ArgumentNullException(nameof(queryDocument));
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Variables = variables
                ?? throw new ArgumentNullException(nameof(variables));

            Fragments = new FragmentCollection(schema, queryDocument);
            _fieldCollector = new FieldCollector(schema, variables, Fragments);
            OperationType = schema.GetOperationType(operation.Operation);
            RootValue = ResolveRootValue(request.Services, schema,
                OperationType, request.InitialValue);
        }

        public ISchema Schema { get; }

        public IReadOnlySchemaOptions Options => Schema.Options;

        public IServiceProvider Services { get; }

        public object RootValue { get; }

        public DocumentNode QueryDocument { get; }

        public OperationDefinitionNode Operation { get; }

        public ObjectType OperationType { get; }

        public FragmentCollection Fragments { get; }

        public VariableCollection Variables { get; }

        public IDataLoaderState DataLoaders { get; }

        public IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType objectType, SelectionSetNode selectionSet)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            return _fieldCollector.CollectFields(
                objectType, selectionSet, ReportError);
        }

        public void ReportError(IQueryError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            _errors.Add(error);
        }

        public IEnumerable<IQueryError> GetErrors()
        {
            return _errors;
        }

        private static object ResolveRootValue(
            IServiceProvider serviceProvider,
            ISchema schema,
            ObjectType operationType,
            object initialValue)
        {
            if (initialValue == null && schema.TryGetNativeType(
               operationType.Name, out Type nativeType))
            {
                initialValue = serviceProvider.GetService(nativeType)
                    ?? Activator.CreateInstance(nativeType);
            }
            return initialValue;
        }
    }
}
