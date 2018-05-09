using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public class ResolverContext
        : IResolverContext
    {
        private readonly SchemaContextInfo _schemaContext;
        private readonly QueryContextInfo _queryContext;
        private readonly ImmutableStack<object> _path;
        private readonly Func<Type, object> _getService;

        internal ResolverContext(
            SchemaContextInfo schemaContext,
            QueryContextInfo queryContext,
            ImmutableStack<object> path,
            Func<Type, object> getService)
        {
            if (schemaContext == null)
            {
                throw new ArgumentNullException(nameof(schemaContext));
            }

            if (queryContext == null)
            {
                throw new ArgumentNullException(nameof(queryContext));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (getService == null)
            {
                throw new ArgumentNullException(nameof(getService));
            }

            _schemaContext = schemaContext;
            _queryContext = queryContext;
            _getService = getService;
        }

        // schema context
        public Schema Schema => _schemaContext.Schema;

        public ObjectType ObjectType => _schemaContext.ObjectType;

        public Field Field => _schemaContext.Field;

        // query context
        public DocumentNode QueryDocument => _queryContext.QueryDocument;

        public OperationDefinitionNode OperationDefinition => _queryContext.OperationDefinition;

        public FieldNode FieldSelection => _queryContext.FieldSelection;

        // execution context
        public ImmutableStack<object> Path => _path;

        public T Parent<T>()
        {
            return (T)_path.Peek();
        }

        public T Argument<T>(string name)
        {
            return (T)_queryContext.Arguments[name];
        }

        public T Service<T>()
        {
            return (T)_getService(typeof(T));
        }

        public static ResolverContext Create(
            ResolverContext parentContext,
            ObjectType objectType,
            object objectValue,
            Field field,
            FieldNode fieldSelection,
            Dictionary<string, object> argumentValues)
        {
            SchemaContextInfo schemaContext = new SchemaContextInfo
            (
                parentContext.Schema,
                objectType,
                field
            );

            QueryContextInfo queryContext = new QueryContextInfo
            (
                parentContext.QueryDocument,
                parentContext.OperationDefinition,
                fieldSelection,
                argumentValues
            );

            return new ResolverContext(
                schemaContext, queryContext,
                parentContext.Path.Push(objectValue),
                parentContext._getService);
        }
    }

}
