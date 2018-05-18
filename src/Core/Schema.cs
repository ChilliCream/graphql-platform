using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public class Schema
    {
        private readonly SchemaContext _context;

        private Schema(SchemaContext context)
        {
            _context = context;
            Query = (ObjectType)context.GetType("Query"); // TODO : rework
        }

        public ObjectType Query { get; }

        public ObjectType Mutation => throw new NotImplementedException();

        public ObjectType Subscription => throw new NotImplementedException();

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
