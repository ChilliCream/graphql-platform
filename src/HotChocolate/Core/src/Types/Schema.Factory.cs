using System;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate
{
    public partial class Schema
    {
        public static Schema Create(
            string schema,
            Action<ISchemaConfiguration> configure)
        {
            if (string.IsNullOrEmpty(schema))
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return Create(Utf8GraphQLParser.Parse(schema), configure);
        }

        public static Schema Create(
            DocumentNode schemaDocument,
            Action<ISchemaConfiguration> configure)
        {
            if (schemaDocument is null)
            {
                throw new ArgumentNullException(nameof(schemaDocument));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            SchemaBuilder builder = CreateSchemaBuilder(configure);
            builder.AddDocument(sp => schemaDocument);
            return builder.Create();
        }

        public static Schema Create(
            Action<ISchemaConfiguration> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            SchemaBuilder builder = CreateSchemaBuilder(configure);
            return builder.Create();
        }

        private static SchemaBuilder CreateSchemaBuilder(
            Action<ISchemaConfiguration> configure)
        {
            var configuration = new SchemaConfiguration();
            configure(configuration);

            return configuration.CreateBuilder();
        }
    }
}
