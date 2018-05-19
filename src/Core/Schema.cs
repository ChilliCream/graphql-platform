using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    /// <summary>
    /// A GraphQL Schema defines the capabilities of a GraphQL server. It
    /// exposes all available types and directives on the server, as well as
    /// the entry points for query, mutation, and subscription operations.
    /// </summary>
    public class Schema
    {
        private readonly SchemaContext _context;

        private Schema(SchemaContext context)
        {
            _context = context;
            QueryType = (ObjectType)context.GetType("Query"); // TODO : rework
        }

        /// <summary>
        /// The type that query operations will be rooted at.
        /// </summary>
        public ObjectType QueryType { get; }

        /// <summary>
        /// If this server supports mutation, the type that
        /// mutation operations will be rooted at.
        /// </summary>
        public ObjectType MutationType => throw new NotImplementedException();

        /// <summary>
        /// If this server support subscription, the type that
        /// subscription operations will be rooted at.
        /// </summary>
        public ObjectType SubscriptionType => throw new NotImplementedException();

        public INamedType GetType(string typeName)
        {
            return _context.GetType(typeName);
        }

        public T GetType<T>(string typeName) where T : INamedType
        {
            return _context.GetType<T>(typeName);
        }

        public IReadOnlyCollection<INamedType> GetAllTypes()
        {
            return _context.GetAllTypes();
        }

        public static Schema Create(
            string schema,
            Action<ISchemaConfiguration> configure)
        {
            DocumentNode schemaDocument = Parser.Default.Parse(schema);
            return Create(schemaDocument, configure);
        }

        public static Schema Create(
            DocumentNode schemaDocument,
            Action<ISchemaConfiguration> configure)
        {
            SchemaContext context = new SchemaContext(
                CreateSystemTypes());

            // deserialize schema objects
            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor(context);
            visitor.Visit(schemaDocument);

            // configure resolvers and aliases
            SchemaConfiguration configuration = new SchemaConfiguration();
            configure(configuration);
            configuration.Commit(context);

            return new Schema(context);
        }

        private static IEnumerable<INamedType> CreateSystemTypes()
        {
            yield return new StringType();
        }
    }
}
