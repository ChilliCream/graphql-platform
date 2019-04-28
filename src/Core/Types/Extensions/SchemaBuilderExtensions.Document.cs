using System.Reflection.Metadata;
using System;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddDocumentFromString(
            this ISchemaBuilder builder,
            string schema)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(schema))
            {
                // TODO : resources
                throw new ArgumentException("message", nameof(schema));
            }

            DocumentNode document = Utf8GraphQLParser.Parse(schema);
            return builder.AddDocument(sp => document);
        }

        public static ISchemaBuilder AddDocument(
            this ISchemaBuilder builder,
            DocumentNode document)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return builder.AddDocument(sp => document);
        }
    }
}
