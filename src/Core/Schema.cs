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

        internal Field SchemaField
        {
            // TODO : move initialized field to a class var
            get => new Field(new FieldConfig
            {
                Name = "__schema",
                Description = "Access the current type schema of this server.",
                Type = () => new NonNullType(_context.GetOutputType("__Schema")),
                Resolver = () => (ctx, ct) => ctx.Schema
            });
        }

        internal Field TypeField
        {
            // TODO : move initialized field to a class var
            get => new Field(new FieldConfig
            {
                Name = "__type",
                Description = "Request the type information of a single type.",
                Type = () => _context.GetOutputType("__Type"),
                Arguments = new[]
                {
                    new InputField(new InputFieldConfig
                    {
                        Name ="type",
                        Type = () => _context.NonNullStringType()
                    })
                },
                Resolver = () => (ctx, ct) => ctx.Schema.GetType(ctx.Argument<string>("type"))
            });
        }

        internal Field TypeNameField
        {
            // TODO : move initialized field to a class var
            get => new Field(new FieldConfig
            {
                Name = "__typename",
                Description = "The name of the current Object type at runtime.",
                Type = () => _context.NonNullStringType(),
                Resolver = () => (ctx, ct) => ctx.ObjectType.Name
            });
        }

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

        public static Schema Create(
            Action<ISchemaConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            SchemaContext context = new SchemaContext(
                CreateSystemTypes());

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
