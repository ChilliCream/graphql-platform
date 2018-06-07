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

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            FragmentCollection fragments =
                new FragmentCollection(schema, queryDocument);

            _services = services;
            _fieldCollector = new FieldCollector(schema, variables, fragments);

            Schema = schema;
            QueryDocument = queryDocument;
            Operation = operation;
            Variables = variables;
            RootValue = rootValue;
            UserContext = userContext;

            Data = new OrderedDictionary();
            Fragments = fragments;
            Errors = new List<IQueryError>();
            NextBatch = new List<FieldResolverTask>();
        }

        public Schema Schema { get; }
        public FragmentCollection Fragments { get; }
        public object RootValue { get; }
        public object UserContext { get; }
        public DocumentNode QueryDocument { get; }
        public OperationDefinitionNode Operation { get; }
        public VariableCollection Variables { get; }
        public OrderedDictionary Data { get; }
        public List<IQueryError> Errors { get; }
        public List<FieldResolverTask> NextBatch { get; }

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
