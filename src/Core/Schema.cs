using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate
{
    /// <summary>
    /// A GraphQL Schema defines the capabilities of a GraphQL server. It
    /// exposes all available types and directives on the server, as well as
    /// the entry points for query, mutation, and subscription operations.
    /// </summary>
    public class Schema
    {
        private readonly SchemaTypes _types;
        private readonly IntrospectionFields _introspectionFields;

        private Schema(SchemaTypes types, IntrospectionFields introspectionFields)
        {
            _types = types;
            _introspectionFields = introspectionFields;
        }

        /// <summary>
        /// The type that query operations will be rooted at.
        /// </summary>
        public ObjectType QueryType => _types.QueryType;

        /// <summary>
        /// If this server supports mutation, the type that
        /// mutation operations will be rooted at.
        /// </summary>
        public ObjectType MutationType => _types.MutationType;

        /// <summary>
        /// If this server support subscription, the type that
        /// subscription operations will be rooted at.
        /// </summary>
        public ObjectType SubscriptionType => _types.SubscriptionType;

        internal __SchemaField SchemaField => _introspectionFields.SchemaField;

        internal __TypeField TypeField => _introspectionFields.TypeField;

        internal __TypeNameField TypeNameField => _introspectionFields.TypeNameField;

        public T GetType<T>(string typeName)
            where T : INamedType
        {
            return _types.GetType<T>(typeName);
        }

        public bool TryGetType<T>(string typeName, out T type)
            where T : INamedType
        {
            return _types.TryGetType<T>(typeName, out type);
        }

        public IReadOnlyCollection<INamedType> GetAllTypes()
        {
            return _types.GetTypes();
        }

        // TODO : Introduce directive type
        internal IReadOnlyCollection<object> GetDirectives()
        {
            return Array.Empty<object>();
        }

        internal IReadOnlyCollection<ObjectType> GetPossibleTypes(IType abstractType)
        {
            return Array.Empty<ObjectType>();
        }

        internal bool TryGetNativeType(string typeName, out Type nativeType)
        {
            nativeType = null;
            return false;
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
            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor(context.Types);
            visitor.Visit(schemaDocument);

            SchemaNames names = new SchemaNames(
                visitor.QueryTypeName,
                visitor.MutationTypeName,
                visitor.SubscriptionTypeName);

            return CreateSchema(context, names, configure, strict);
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
            return CreateSchema(context, default(SchemaNames), configure, strict);
        }

        private static Schema CreateSchema(
            SchemaContext context,
            SchemaNames names,
            Action<ISchemaConfiguration> configure,
            bool strict)
        {
            List<SchemaError> errors = new List<SchemaError>();

            // setup introspection fields
            IntrospectionFields introspectionFields =
                new IntrospectionFields(context, e => errors.Add(e));

            try
            {
                // configure resolvers, custom types and type mappings.
                SchemaConfiguration configuration = new SchemaConfiguration();
                configure(configuration);
                errors.AddRange(configuration.RegisterTypes(context));
                configuration.RegisterResolvers(context);
                errors.AddRange(context.CompleteTypes());
            }
            catch (ArgumentException ex)
            {
                // TODO : maybe we should throw a more specific
                // argument exception that at least contains the config object.
                throw new SchemaException(new[]
                {
                    new SchemaError(ex.Message, null)
                });
            }

            if (strict && errors.Any())
            {
                throw new SchemaException(errors);
            }

            SchemaNames n = string.IsNullOrEmpty(names.QueryTypeName)
                ? new SchemaNames(null, null, null)
                : names;

            if (strict && !context.Types.TryGetType<ObjectType>(
                n.QueryTypeName, out ObjectType ot))
            {
                throw new SchemaException(new SchemaError(
                    "Schema is missing the mandatory `Query` type."));
            }

            return new Schema(
                SchemaTypes.Create(context.Types.GetTypes(), n),
                introspectionFields);
        }

        private static SchemaContext CreateSchemaContext()
        {
            // create context with system types
            SchemaContext context = new SchemaContext();
            context.Types.RegisterType(typeof(StringType));
            context.Types.RegisterType(typeof(BooleanType));
            context.Types.RegisterType(typeof(IntType));

            // register introspection types
            context.Types.RegisterType(typeof(__Directive));
            context.Types.RegisterType(typeof(__DirectiveLocation));
            context.Types.RegisterType(typeof(__EnumValue));
            context.Types.RegisterType(typeof(__Field));
            context.Types.RegisterType(typeof(__InputValue));
            context.Types.RegisterType(typeof(__Schema));
            context.Types.RegisterType(typeof(__Type));
            context.Types.RegisterType(typeof(__TypeKind));

            return context;
        }

        private sealed class SchemaTypes
        {
            public Dictionary<string, INamedType> _types;

            private SchemaTypes(
                IEnumerable<INamedType> types,
                string queryTypeName,
                string mutationTypeName,
                string subscriptionTypeName)
            {
                _types = types.ToDictionary(t => t.Name);

                INamedType namedType;
                if (_types.TryGetValue(queryTypeName, out namedType)
                    && namedType is ObjectType queryType)
                {
                    QueryType = queryType;
                }

                if (_types.TryGetValue(mutationTypeName, out namedType)
                   && namedType is ObjectType mutationType)
                {
                    MutationType = mutationType;
                }

                if (_types.TryGetValue(subscriptionTypeName, out namedType)
                   && namedType is ObjectType subscriptionType)
                {
                    SubscriptionType = subscriptionType;
                }
            }

            public ObjectType QueryType { get; }
            public ObjectType MutationType { get; }
            public ObjectType SubscriptionType { get; }

            public T GetType<T>(string typeName) where T : IType
            {
                if (_types.TryGetValue(typeName, out INamedType namedType)
                    && namedType is T type)
                {
                    return type;
                }

                throw new ArgumentException(
                    "The specified type does not exist or " +
                    "is not of the specified kind.",
                    nameof(typeName));
            }

            public bool TryGetType<T>(string typeName, out T type) where T : IType
            {
                if (_types.TryGetValue(typeName, out INamedType namedType)
                    && namedType is T t)
                {
                    type = t;
                    return true;
                }

                type = default(T);
                return false;
            }

            public IReadOnlyCollection<INamedType> GetTypes()
            {
                return _types.Values;
            }

            public static SchemaTypes Create(
                IEnumerable<INamedType> types,
                SchemaNames names)
            {
                if (types == null)
                {
                    throw new ArgumentNullException(nameof(types));
                }

                SchemaNames n = string.IsNullOrEmpty(names.QueryTypeName)
                    ? new SchemaNames(null, null, null)
                    : names;

                return new SchemaTypes(types,
                    n.QueryTypeName,
                    n.MutationTypeName,
                    n.SubscriptionTypeName);
            }
        }

        private sealed class IntrospectionFields
        {
            public IntrospectionFields(SchemaContext context, Action<SchemaError> reportError)
            {
                SchemaField = new __SchemaField();
                TypeField = new __TypeField();
                TypeNameField = new __TypeNameField();

                SchemaField.RegisterDependencies(context, reportError, null);
                TypeField.RegisterDependencies(context, reportError, null);
                TypeNameField.RegisterDependencies(context, reportError, null);

                SchemaField.CompleteField(context, reportError, null);
                TypeField.CompleteField(context, reportError, null);
                TypeNameField.CompleteField(context, reportError, null);
            }

            internal __SchemaField SchemaField { get; }

            internal __TypeField TypeField { get; }

            internal __TypeNameField TypeNameField { get; }
        }

        private readonly struct SchemaNames
        {
            public SchemaNames(
                string queryTypeName,
                string mutationTypeName,
                string subscriptionTypeName)
            {
                QueryTypeName = string.IsNullOrEmpty(queryTypeName)
                    ? "Query" : queryTypeName;
                MutationTypeName = string.IsNullOrEmpty(mutationTypeName)
                    ? "Mutation" : mutationTypeName;
                SubscriptionTypeName = string.IsNullOrEmpty(subscriptionTypeName)
                    ? "Subscription" : subscriptionTypeName;
            }

            public string QueryTypeName { get; }
            public string MutationTypeName { get; }
            public string SubscriptionTypeName { get; }
        }
    }
}
