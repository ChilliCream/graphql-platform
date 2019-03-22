using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Configuration;

namespace HotChocolate
{
    public class SchemaBuilder
        : ISchemaBuilder
    {
        private readonly List<FieldMiddleware> _globalComponents =
            new List<FieldMiddleware>();
        private readonly List<LoadSchemaDocument> _documents =
            new List<LoadSchemaDocument>();
        private readonly List<ITypeReference> _types =
            new List<ITypeReference>();
        private readonly Dictionary<OperationType, ITypeReference> _operations =
            new Dictionary<OperationType, ITypeReference>();
        private readonly Dictionary<FieldReference, FieldResolver> _resolvers =
            new Dictionary<FieldReference, FieldResolver>();
        private string _description;
        private IReadOnlySchemaOptions _options = new SchemaOptions();
        private IServiceProvider _services;

        public ISchemaBuilder SetDescription(string description)
        {
            _description = description;
            return this;
        }

        public ISchemaBuilder SetOptions(IReadOnlySchemaOptions options)
        {
            _options = options ?? new SchemaOptions();
            return this;
        }

        public ISchemaBuilder AddDirective<T>(T directiveInstance)
            where T : class
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder AddDirective<T>()
            where T : class, new()
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder AddDirective(
            NameString name,
            params ArgumentNode[] arguments)
        {
            throw new NotImplementedException();
        }

        public ISchemaBuilder Use(FieldMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _globalComponents.Add(middleware);
            return this;
        }

        public ISchemaBuilder AddDocument(LoadSchemaDocument loadSchemaDocument)
        {
            if (loadSchemaDocument == null)
            {
                throw new ArgumentNullException(nameof(loadSchemaDocument));
            }

            _documents.Add(loadSchemaDocument);
            return this;
        }

        public ISchemaBuilder AddType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _types.Add(new ClrTypeReference(
                type,
                SchemaTypeReference.InferTypeContext(type)));

            return this;
        }

        public ISchemaBuilder AddType(INamedType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _types.Add(new SchemaTypeReference(type));
            return this;
        }

        public ISchemaBuilder AddDirectiveType(DirectiveType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _types.Add(new SchemaTypeReference(type));
            return this;
        }

        public ISchemaBuilder AddRootType(
            Type type,
            OperationType operation)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsClass)
            {
                // TODO : resources
                throw new ArgumentException(
                    "Root type must be a class",
                     nameof(type));
            }

            if (BaseTypes.IsNonGenericBaseType(type))
            {
                // TODO : resources
                throw new ArgumentException(
                    "Non-generic schema types are not allowed.",
                     nameof(type));
            }

            if (BaseTypes.IsSchemaType(type)
                && !typeof(ObjectType).IsAssignableFrom(type))
            {
                // TODO : resources
                throw new ArgumentException(
                    "must be object type",
                     nameof(type));
            }

            var reference = new ClrTypeReference(type, TypeContext.Output);
            _operations.Add(operation, reference);
            _types.Add(reference);
            return this;
        }

        public ISchemaBuilder AddRootType(
            ObjectType type,
            OperationType operation)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var reference = new SchemaTypeReference((ITypeSystemObject)type);
            _operations.Add(operation, reference);
            _types.Add(reference);
            return this;
        }

        public ISchemaBuilder AddResolver(FieldResolver fieldResolver)
        {
            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            _resolvers.Add(fieldResolver.ToFieldReference(), fieldResolver);
            return this;
        }

        public ISchemaBuilder AddServices(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (_services == null)
            {
                _services = services;
            }
            else
            {
                _services = _services.Include(services);
            }

            return this;
        }

        public ISchema Create()
        {
            TypeInitializer initializer = InitializeTypes();
            SchemaDefinition definition = CreateSchemaDefinition(initializer);
            return new Schema(definition);
        }

        private TypeInitializer InitializeTypes()
        {
            var service = _services ?? new EmptyServiceProvider();
            var initializer = new TypeInitializer(service, _types, IsQueryType);

            foreach (FieldMiddleware component in _globalComponents)
            {
                initializer.GlobalComponents.Add(component);
            }

            foreach (FieldReference reference in _resolvers.Keys)
            {
                initializer.Resolvers[reference] = new RegisteredResolver(
                    typeof(object), _resolvers[reference]);
            }

            initializer.Initialize();
            return initializer;
        }

        private SchemaDefinition CreateSchemaDefinition(
            TypeInitializer initializer)
        {
            var definition = new SchemaDefinition();

            definition.Description = _description;
            definition.Options = _options;
            definition.Services = _services;

            definition.Types = initializer.Types.Values
                .Select(t => t.Type).OfType<INamedType>().ToList();
            definition.DirectiveTypes = initializer.Types.Values
                .Select(t => t.Type).OfType<DirectiveType>().ToList();

            definition.QueryType = ResolveOperation(
                initializer, OperationType.Query);
            definition.MutationType = ResolveOperation(
                initializer, OperationType.Mutation);
            definition.SubscriptionType = ResolveOperation(
                initializer, OperationType.Subscription);

            return definition;
        }

        private ObjectType ResolveOperation(
            TypeInitializer initializer,
            OperationType operation)
        {
            if (!_operations.ContainsKey(operation))
            {
                NameString typeName = operation.ToString();
                return initializer.Types.Values
                    .Select(t => t.Type)
                    .OfType<ObjectType>()
                    .FirstOrDefault(t => t.Name.Equals(typeName));
            }
            else if (_operations.TryGetValue(operation,
                out ITypeReference reference)
                && initializer.TryNormalizeReference(reference,
                out ITypeReference normalized)
                && initializer.Types.TryGetValue(normalized,
                out RegisteredType type)
                && type.Type is ObjectType rootType)
            {
                return rootType;
            }
            return null;
        }

        private bool IsQueryType(TypeSystemObjectBase type)
        {
            if (_operations.TryGetValue(OperationType.Query,
                out ITypeReference reference))
            {
                if (reference is ISchemaTypeReference sr)
                {
                    return sr.Type == type;
                }

                if (reference is IClrTypeReference cr)
                {
                    return cr.Type == type.GetType();
                }
            }
            else
            {
                // TODO : query to constant
                return type is ObjectType
                    && type.Name.Equals("Query");
            }

            return false;
        }

        public static SchemaBuilder New() => new SchemaBuilder();
    }
}
