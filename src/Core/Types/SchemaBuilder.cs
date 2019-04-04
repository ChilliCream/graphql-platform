using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Configuration;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Types.Factories;

namespace HotChocolate
{
    public partial class SchemaBuilder
        : ISchemaBuilder
    {
        private readonly List<FieldMiddleware> _globalComponents =
            new List<FieldMiddleware>();
        private readonly List<LoadSchemaDocument> _documents =
            new List<LoadSchemaDocument>();
        private readonly List<ITypeReference> _types =
            new List<ITypeReference>();
        private readonly List<Type> _resolverTypes = new List<Type>();
        private readonly Dictionary<OperationType, ITypeReference> _operations =
            new Dictionary<OperationType, ITypeReference>();
        private readonly Dictionary<FieldReference, FieldResolver> _resolvers =
            new Dictionary<FieldReference, FieldResolver>();
        private readonly IBindingCompiler _bindingCompiler =
            new BindingCompiler();
        private string _description;
        private SchemaOptions _options = new SchemaOptions();
        private IsOfTypeFallback _isOfType;
        private IServiceProvider _services;

        public ISchemaBuilder SetDescription(string description)
        {
            _description = description;
            return this;
        }

        public ISchemaBuilder SetOptions(IReadOnlySchemaOptions options)
        {
            if (options != null)
            {
                _options = SchemaOptions.FromOptions(options);
            }
            return this;
        }

        public ISchemaBuilder ModifyOptions(Action<ISchemaOptions> configure)
        {
            configure(_options);
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

            if (type.IsDefined(typeof(GraphQLResolverOfAttribute), true))
            {
                AddResolverType(type);
            }
            else
            {
                _types.Add(new ClrTypeReference(
                    type,
                    SchemaTypeReference.InferTypeContext(type)));
            }

            return this;
        }

        private void AddResolverType(Type type)
        {
            GraphQLResolverOfAttribute attribute =
                type.GetCustomAttribute<GraphQLResolverOfAttribute>(true);

            _resolverTypes.Add(type);

            if (attribute.Types != null)
            {
                foreach (Type schemaType in attribute.Types)
                {
                    if (typeof(ObjectType).IsAssignableFrom(schemaType)
                        && !BaseTypes.IsNonGenericBaseType(schemaType))
                    {
                        _types.Add(new ClrTypeReference(
                            schemaType,
                            SchemaTypeReference.InferTypeContext(schemaType)));
                    }
                }
            }
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

        public ISchemaBuilder AddBinding(IBindingInfo binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (!binding.IsValid())
            {
                // TODO : resources
                throw new ArgumentException(
                    "binding is not valid",
                    nameof(binding));
            }

            if (!_bindingCompiler.CanHandle(binding))
            {
                // TODO : resources
                throw new ArgumentException(
                    "cannot handle binding",
                    nameof(binding));
            }

            _bindingCompiler.AddBinding(binding);
            return this;
        }

        public ISchemaBuilder SetTypeResolver(IsOfTypeFallback isOfType)
        {
            _isOfType = isOfType;
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

        public Schema Create()
        {
            IServiceProvider services = _services ?? new EmptyServiceProvider();
            IBindingLookup bindingLookup =
                _bindingCompiler.Compile(DescriptorContext.Create(services));

            var types = new List<ITypeReference>(_types);

            if (_documents.Count > 0)
            {
                types.AddRange(ParseDocuments(services, bindingLookup));
            }

            var lazy = new LazySchema();

            TypeInitializer initializer =
                InitializeTypes(services, bindingLookup, types, () => lazy.Schema);

            SchemaDefinition definition = CreateSchemaDefinition(initializer);

            if (definition.QueryType == null && _options.StrictValidation)
            {
                // TODO : Resources
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("No QUERY TYPE")
                        .Build());
            }

            var schema = new Schema(definition);
            lazy.Schema = schema;
            return schema;
        }

        ISchema ISchemaBuilder.Create() => Create();

        private IEnumerable<ITypeReference> ParseDocuments(
            IServiceProvider services,
            IBindingLookup bindingLookup)
        {
            var types = new List<ITypeReference>();

            foreach (LoadSchemaDocument fetchSchema in _documents)
            {
                // TODO: retrieve root type names
                DocumentNode schemaDocument = fetchSchema(services);

                var visitor = new SchemaSyntaxVisitor(bindingLookup);
                visitor.Visit(schemaDocument, null);
                types.AddRange(visitor.Types);

                RegisterOperationName(OperationType.Query,
                    visitor.QueryTypeName);
                RegisterOperationName(OperationType.Mutation,
                    visitor.MutationTypeName);
                RegisterOperationName(OperationType.Subscription,
                    visitor.SubscriptionTypeName);
            }

            return types;
        }

        private void RegisterOperationName(
            OperationType operation,
            string typeName)
        {
            if (!_operations.ContainsKey(operation)
                && !string.IsNullOrEmpty(typeName))
            {
                _operations.Add(operation,
                    new SyntaxTypeReference(
                        new NamedTypeNode(typeName),
                        TypeContext.Output));
            }
        }

        private TypeInitializer InitializeTypes(
            IServiceProvider services,
            IBindingLookup bindingLookup,
            IEnumerable<ITypeReference> types,
            Func<ISchema> schemaResolver)
        {
            var initializer = new TypeInitializer(
                services, types, _resolverTypes, IsQueryType);

            foreach (FieldMiddleware component in _globalComponents)
            {
                initializer.GlobalComponents.Add(component);
            }

            foreach (FieldReference reference in _resolvers.Keys)
            {
                initializer.Resolvers[reference] = new RegisteredResolver(
                    typeof(object), _resolvers[reference]);
            }

            foreach (RegisteredResolver resolver in bindingLookup.Bindings
                .SelectMany(t => t.CreateResolvers()))
            {
                var reference = new FieldReference(
                    resolver.Field.TypeName,
                    resolver.Field.FieldName);
                initializer.Resolvers[reference] = resolver;
            }

            initializer.Initialize(schemaResolver);
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

            RegisterOperationName(OperationType.Query,
                _options.QueryTypeName);
            RegisterOperationName(OperationType.Mutation,
                _options.MutationTypeName);
            RegisterOperationName(OperationType.Subscription,
                _options.SubscriptionTypeName);

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
                out ITypeReference reference))
            {

                if (reference is ISchemaTypeReference sr)
                {
                    return (ObjectType)sr.Type;
                }

                if (reference is IClrTypeReference cr
                    && initializer.TryGetRegisteredType(cr,
                    out RegisteredType registeredType))
                {
                    return (ObjectType)registeredType.Type;
                }

                if (reference is ISyntaxTypeReference str)
                {
                    NamedTypeNode namedType = str.Type.NamedType();
                    return initializer.Types.Values.Select(t => t.Type)
                        .OfType<ObjectType>()
                        .FirstOrDefault(t => t.Name.Equals(
                            namedType.Name.Value));
                }
            }

            return null;
        }

        private bool IsQueryType(TypeSystemObjectBase type)
        {
            if (type is ObjectType objectType)
            {
                if (_operations.TryGetValue(OperationType.Query,
                    out ITypeReference reference))
                {
                    if (reference is ISchemaTypeReference sr)
                    {
                        return sr.Type == objectType;
                    }

                    if (reference is IClrTypeReference cr)
                    {
                        return cr.Type == objectType.GetType();
                    }

                    if (reference is ISyntaxTypeReference str)
                    {
                        return objectType.Name.Equals(
                            str.Type.NamedType().Name.Value);
                    }
                }
                else
                {
                    // TODO : query to constant
                    return type is ObjectType
                        && type.Name.Equals("Query");
                }
            }

            return false;
        }

        public static SchemaBuilder New() => new SchemaBuilder();
    }
}
