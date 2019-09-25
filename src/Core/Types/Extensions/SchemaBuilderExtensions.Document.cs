using System;
using System.IO;
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

            return builder.AddDocument(sp => Utf8GraphQLParser.Parse(schema));
        }

        public static ISchemaBuilder AddDocumentFromFile(
            this ISchemaBuilder builder,
            string filePath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(
                    "",
                    nameof(filePath));
            }

            return builder.AddDocument(sp =>
                Utf8GraphQLParser.Parse(
                    File.ReadAllBytes(filePath)));
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
