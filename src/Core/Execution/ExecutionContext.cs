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
        private readonly IServiceProvider _serviceProvider = null;
        private readonly StateObjectContainer<string> _dataLoaders;
        private readonly StateObjectContainer<Type> _state;
        private readonly FieldCollector _fieldCollector;

        public ExecutionContext(
            ISchema schema,
            DocumentNode queryDocument,
            OperationDefinitionNode operation,
            VariableCollection variables,
            object rootValue)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            QueryDocument = queryDocument
                ?? throw new ArgumentNullException(nameof(queryDocument));
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Variables = variables
                ?? throw new ArgumentNullException(nameof(variables));

            Fragments = new FragmentCollection(schema, queryDocument);
            _fieldCollector = new FieldCollector(schema, variables, Fragments);
            OperationType = schema.GetOperationType(operation.Operation);
            RootValue = ResolveRootValue(serviceProvider, schema, OperationType, rootValue);
        }

        public ISchema Schema { get; }

        public IReadOnlySchemaOptions Options => Schema.Options;

        public object RootValue { get; }

        public DocumentNode QueryDocument { get; }

        public OperationDefinitionNode Operation { get; }

        public ObjectType OperationType { get; }

        public FragmentCollection Fragments { get; }

        public VariableCollection Variables { get; }

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

        public T GetDataLoader<T>(string key)
        {
            return (T)_dataLoaders.GetStateObject(key);
        }

        public T GetState<T>()
        {
            return (T)_state.GetStateObject(typeof(T));
        }
    }
}
