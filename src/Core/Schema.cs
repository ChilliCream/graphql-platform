using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Introspection;
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
            QueryType = (ObjectType)context.GetType(TypeNames.Query);

            if (context.TryGetOutputType<ObjectType>(
                TypeNames.Mutation, out ObjectType mutationType))
            {
                MutationType = mutationType;
            }

            if (context.TryGetOutputType<ObjectType>(
                TypeNames.Mutation, out ObjectType subscriptionType))
            {
                SubscriptionType = subscriptionType;
            }

            SchemaField = IntrospectionTypes.CreateSchemaField(context);
            TypeField = IntrospectionTypes.CreateTypeField(context);
            TypeNameField = IntrospectionTypes.CreateTypeNameField(context);
        }

        /// <summary>
        /// The type that query operations will be rooted at.
        /// </summary>
        public ObjectType QueryType { get; }

        /// <summary>
        /// If this server supports mutation, the type that
        /// mutation operations will be rooted at.
        /// </summary>
        public ObjectType MutationType { get; }

        /// <summary>
        /// If this server support subscription, the type that
        /// subscription operations will be rooted at.
        /// </summary>
        public ObjectType SubscriptionType { get; }

        internal Field SchemaField { get; }

        internal Field TypeField { get; }

        internal Field TypeNameField { get; }

        public INamedType GetType(string typeName)
        {
            return _context.GetType(typeName);
        }

        public T GetType<T>(string typeName)
            where T : INamedType
        {
            return _context.GetType<T>(typeName);
        }

        public IReadOnlyCollection<INamedType> GetAllTypes()
        {
            return _context.GetAllTypes();
        }

        // TODO : Introduce directive type
        public IReadOnlyCollection<object> GetDirectives()
        {
            return Array.Empty<object>();
        }

        public IEnumerable<IType> GetPossibleTypes(IType abstractType)
        {
            throw new NotImplementedException();
        }

        public static Schema Create(
            string schema,
            Action<ISchemaConfiguration> configure)
        {
            return Create(schema, configure, false);
        }

        public static Schema Create(
            string schema,
            Action<ISchemaConfiguration> configure,
            bool strict)
        {
            if (string.IsNullOrEmpty(schema))
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return Create(Parser.Default.Parse(schema), configure, strict);
        }

        public static Schema Create(
            DocumentNode schemaDocument,
            Action<ISchemaConfiguration> configure)
        {
            return Create(schemaDocument, configure, false);
        }

        public static Schema Create(
            DocumentNode schemaDocument,
            Action<ISchemaConfiguration> configure,
            bool strict)
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
            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor(context);
            visitor.Visit(schemaDocument);

            return CreateSchema(context, configure, strict);
        }

        public static Schema Create(
            Action<ISchemaConfiguration> configure)
        {
            return Create(configure, false);
        }

        public static Schema Create(
            Action<ISchemaConfiguration> configure,
            bool strict)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            SchemaContext context = CreateSchemaContext();
            return CreateSchema(context, configure, strict);
        }

        private static Schema CreateSchema(
            SchemaContext context,
            Action<ISchemaConfiguration> configure,
            bool strict)
        {
            try
            {
                // configure resolvers, custom types and type mappings.
                SchemaConfiguration configuration = new SchemaConfiguration();
                configure(configuration);
                configuration.Commit(context);

                // finalize objects and seal the schema context
                List<SchemaError> errors = context.Seal();

                if (strict && errors.Any())
                {
                    throw new SchemaException(errors);
                }

                return new Schema(context);
            }
            catch (ArgumentException ex)
            {
                // TODO : maybe we should throw a more specific argument exception that at least contains the config object.
                throw new SchemaException(new[] { new SchemaError(ex.Message, null) });
            }
        }

        private static SchemaContext CreateSchemaContext()
        {
            // create context with system types
            SchemaContext context = new SchemaContext(CreateSystemTypes());

            // register introspection types
            RegisterIntrospectionTypes(context);

            return context;
        }

        private static void RegisterIntrospectionTypes(SchemaContext context)
        {
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.RegisterType(IntrospectionTypes.__Directive);
            configuration.RegisterType(IntrospectionTypes.__DirectiveLocation);
            configuration.RegisterType(IntrospectionTypes.__EnumValue);
            configuration.RegisterType(IntrospectionTypes.__Field);
            configuration.RegisterType(IntrospectionTypes.__InputValue);
            configuration.RegisterType(IntrospectionTypes.__Schema);
            configuration.RegisterType(IntrospectionTypes.__Type);
            configuration.RegisterType(IntrospectionTypes.__TypeKind);
            configuration.Commit(context);
        }

        private static IEnumerable<INamedType> CreateSystemTypes()
        {
            yield return new StringType();
            yield return new BooleanType();
            yield return new IntegerType();
        }
    }
}
