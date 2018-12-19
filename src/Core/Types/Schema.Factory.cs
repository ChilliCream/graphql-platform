using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

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

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return Create(Parser.Default.Parse(schema), configure);
        }

        public static Schema Create(
            DocumentNode schemaDocument,
            Action<ISchemaConfiguration> configure)
        {
            if (schemaDocument == null)
            {
                throw new ArgumentNullException(nameof(schemaDocument));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            SchemaContext context = SchemaContextFactory.Create();

            // deserialize schema objects
            var visitor = new SchemaSyntaxVisitor(context.Types);
            visitor.Visit(schemaDocument, null);

            return CreateSchema(context, c =>
            {
                c.Options.QueryTypeName = visitor.QueryTypeName;
                c.Options.MutationTypeName = visitor.MutationTypeName;
                c.Options.SubscriptionTypeName = visitor.SubscriptionTypeName;

                configure(c);
            });
        }

        public static Schema Create(
            Action<ISchemaConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            SchemaContext context = SchemaContextFactory.Create();
            return CreateSchema(context, configure);
        }

        private static Schema CreateSchema(
            SchemaContext context,
            Action<ISchemaConfiguration> configure)
        {
            var errors = new List<SchemaError>();

            IReadOnlySchemaOptions options = ExecuteSchemaConfiguration(
                context, configure, errors);


            if (!context.Types.TryGetType(
                options.QueryTypeName, out ObjectType ot))
            {
                errors.Add(new SchemaError(
                    "Schema is missing the mandatory `Query` type."));
            }

            if (options.StrictValidation && errors.Any())
            {
                throw new SchemaException(errors);
            }

            return new Schema(
                context.Services ?? new EmptyServiceProvider(),
                context,
                options);
        }

        private static IReadOnlySchemaOptions ExecuteSchemaConfiguration(
            SchemaContext context,
            Action<ISchemaConfiguration> configure,
            List<SchemaError> errors)
        {
            try
            {
                // configure resolvers, custom types and type mappings.
                var configuration = new SchemaConfiguration(
                    context.RegisterServiceProvider,
                    context.Types,
                    context.Resolvers,
                    context.Directives);

                configuration.RegisterCustomContext<IResolverCache>(
                    ExecutionScope.Global,
                    s => new ResolverCache());

                configure(configuration);

                var options = new ReadOnlySchemaOptions(configuration.Options);
                var typeFinalizer = new TypeFinalizer(configuration);
                typeFinalizer.FinalizeTypes(context, options.QueryTypeName);
                errors.AddRange(typeFinalizer.Errors);

                return options;
            }
            catch (Exception ex)
            {
                throw new SchemaException(new[]
                {
                    new SchemaError(ex.Message, null, ex)
                });
            }
        }
    }
}
