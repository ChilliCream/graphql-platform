using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public class Schema
        : ISchema
    {
        private readonly SchemaContext _context;

        private Schema(SchemaContext context)
        {
            _context = context;
        }

        public INamedType GetType(string typeName)
        {
            return _context.GetType(typeName);
        }

        public T GetType<T>(string typeName) where T : INamedType
        {
            return _context.GetType<T>(typeName);
        }

        public IEnumerator<IType> GetEnumerator()
        {
            return _context.GetAllTypes().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static Schema Create(
            DocumentNode schemaDocument,
            Action<ISchemaConfiguration> configure)
        {
            SchemaContext context = new SchemaContext(
                new INamedType[] { },
                new Dictionary<string, ResolveType>(), null);

            // deserialize schema objects
            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor(context);
            visitor.Visit(schemaDocument);

            // configure resolvers and aliases
            SchemaConfiguration configuration = new SchemaConfiguration();
            configure(configuration);
            configuration.Commit(context);

            return new Schema(context);
        }
    }
}
