using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
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

            SchemaContext context = CreateSchemaContext();

            // deserialize schema objects
            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor(context.Types);
            visitor.Visit(schemaDocument);

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

            SchemaContext context = CreateSchemaContext();
            return CreateSchema(context, configure);
        }

        private static Schema CreateSchema(
            SchemaContext context,
            Action<ISchemaConfiguration> configure)
        {
            List<SchemaError> errors = new List<SchemaError>();

            // setup introspection fields
            IntrospectionFields introspectionFields =
                new IntrospectionFields(context, e => errors.Add(e));

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
                context.ServiceManager,
                SchemaTypes.Create(
                    context.Types.GetTypes(),
                    context.Types.GetTypeBindings(),
                    options),
                options,
                introspectionFields);
        }

        private static IReadOnlySchemaOptions ExecuteSchemaConfiguration(
            SchemaContext context,
            Action<ISchemaConfiguration> configure,
            List<SchemaError> errors)
        {
            try
            {
                // configure resolvers, custom types and type mappings.
                SchemaConfiguration configuration = new SchemaConfiguration(
                    context.ServiceManager.RegisterServiceProvider,
                    context.Types);
                configure(configuration);

                TypeFinalizer typeFinalizer = new TypeFinalizer(configuration);
                typeFinalizer.FinalizeTypes(context);
                errors.AddRange(typeFinalizer.Errors);

                return new ReadOnlySchemaOptions(configuration.Options);
            }
            catch (Exception ex)
            {
                throw new SchemaException(new[]
                {
                    new SchemaError(ex.Message, null, ex)
                });
            }
        }

        private static SchemaContext CreateSchemaContext()
        {
            SchemaContext context = new SchemaContext(new ServiceManager());

            // create context with system types
            context.Types.RegisterType(new TypeReference(typeof(StringType)));
            context.Types.RegisterType(new TypeReference(typeof(BooleanType)));
            context.Types.RegisterType(new TypeReference(typeof(IntType)));
            context.Types.RegisterType(new TypeReference(typeof(FloatType)));

            // register introspection types
            context.Types.RegisterType(new TypeReference(typeof(__Directive)));
            context.Types.RegisterType(new TypeReference(typeof(__DirectiveLocation)));
            context.Types.RegisterType(new TypeReference(typeof(__EnumValue)));
            context.Types.RegisterType(new TypeReference(typeof(__Field)));
            context.Types.RegisterType(new TypeReference(typeof(__InputValue)));
            context.Types.RegisterType(new TypeReference(typeof(__Schema)));
            context.Types.RegisterType(new TypeReference(typeof(__Type)));
            context.Types.RegisterType(new TypeReference(typeof(__TypeKind)));

            return context;
        }
    }
}
