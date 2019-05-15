using System;
using HotChocolate.Language;
using HotChocolate.Properties;

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
                throw new ArgumentException(
                    TypeResources.SchemaBuilderExtensions_SchemaIsEmpty,
                    nameof(schema));
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
