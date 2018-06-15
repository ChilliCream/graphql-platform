using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
        : IServiceProvider
    {
        private readonly IServiceProvider _services;
        private readonly FieldCollector _fieldCollector;

        public ExecutionContext(Schema schema, DocumentNode queryDocument,
            OperationDefinitionNode operation, VariableCollection variables,
            IServiceProvider services, object rootValue, object userContext)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            QueryDocument = queryDocument
                ?? throw new ArgumentNullException(nameof(queryDocument));
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            Variables = variables
                ?? throw new ArgumentNullException(nameof(variables));
            _services = services
                ?? throw new ArgumentNullException(nameof(services));
            RootValue = rootValue;
            UserContext = userContext;

            Fragments = new FragmentCollection(schema, queryDocument);
            _fieldCollector = new FieldCollector(schema, variables, Fragments);
        }

        public Schema Schema { get; }

        public FragmentCollection Fragments { get; }

        public object RootValue { get; }

        public object UserContext { get; }

        public DocumentNode QueryDocument { get; }

        public OperationDefinitionNode Operation { get; }

        public VariableCollection Variables { get; }

        public OrderedDictionary Data { get; } = new OrderedDictionary();

        public List<IQueryError> Errors { get; } = new List<IQueryError>();

        public List<FieldResolverTask> NextBatch { get; } = new List<FieldResolverTask>();

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
                objectType, selectionSet, Errors.Add);
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            return _services.GetService(serviceType);
        }
    }
}
