using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
        : IServiceProvider
    {
        private readonly IServiceProvider _services;

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

            Schema = schema;
            QueryDocument = queryDocument;
            Operation = operation;
            Variables = variables;
            RootValue = rootValue;
            UserContext = userContext;
            _services = services;

            Data = new Dictionary<string, object>();
            Fragments = new FragmentCollection(schema, queryDocument);
            FieldResolver = new FieldResolver(schema, variables, Fragments);
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
        public Dictionary<string, object> Data { get; }
        public List<IQueryError> Errors { get; }
        public List<FieldResolverTask> NextBatch { get; }
        public FieldResolver FieldResolver { get; }

        public object GetService(Type serviceType)
        {
            return _services.GetService(serviceType);
        }
    }
}
