using System.Reflection;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Configuration;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Properties;

namespace HotChocolate
{
    public partial class SchemaBuilder
        : ISchemaBuilder
    {
        private readonly Dictionary<string, object> _contextData =
            new Dictionary<string, object>();
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
        private readonly Dictionary<string, Dictionary<Type, CreateConvention>> _conventions =
            new Dictionary<string, Dictionary<Type, CreateConvention>>();
        private readonly Dictionary<ClrTypeReference, ITypeReference> _clrTypes =
            new Dictionary<ClrTypeReference, ITypeReference>();
        private readonly List<object> _interceptors = new List<object>();
        private readonly List<Action<IDescriptorContext>> _onBeforeCreate =
            new List<Action<IDescriptorContext>>();
        private readonly IBindingCompiler _bindingCompiler =
            new BindingCompiler();
        private SchemaOptions _options = new SchemaOptions();
        private IsOfTypeFallback _isOfType;
        private IServiceProvider _services;
        private ITypeReference _schema;

        public ISchemaBuilder SetSchema(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (typeof(Schema).IsAssignableFrom(type))
            {
                _schema = TypeReference.Create(type, TypeContext.None);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_SchemaTypeInvalid,
                    nameof(type));
            }
            return this;
        }

        public ISchemaBuilder SetSchema(ISchema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (schema is TypeSystemObjectBase)
            {
                _schema = new SchemaTypeReference(schema);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_ISchemaNotTso,
                    nameof(schema));
            }
            return this;
        }

        public ISchemaBuilder SetSchema(Action<ISchemaTypeDescriptor> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _schema = new SchemaTypeReference(new Schema(configure));
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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configure(_options);
            return this;
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
                _types.Add(TypeReference.Create(
                    type,
                    SchemaTypeReference.InferTypeContext(type)));
            }

            return this;
        }

        public ISchemaBuilder AddConvention(
            string scope,
            Type convention,
            CreateConvention factory)
        {
            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (!_conventions.TryGetValue(
                scope,
                out Dictionary<Type, CreateConvention> conventionScopes))
            {
                conventionScopes = new Dictionary<Type, CreateConvention>();
                _conventions[scope] = conventionScopes;
            }

            conventionScopes[convention] = factory;
            return this;
        }

        public ISchemaBuilder BindClrType(Type clrType, Type schemaType)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            if (schemaType == null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            if (!BaseTypes.IsSchemaType(schemaType))
            {
                // TODO : resources
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_MustBeSchemaType,
                    nameof(schemaType));
            }

            TypeContext context =
                SchemaTypeReference.InferTypeContext(schemaType);
            _clrTypes[TypeReference.Create(clrType, context)] =
                TypeReference.Create(schemaType, context);

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
                        _types.Add(TypeReference.Create(
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

        public ISchemaBuilder AddType(INamedTypeExtension type)
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
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_RootType_MustBeClass,
                    nameof(type));
            }

            if (BaseTypes.IsNonGenericBaseType(type))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_RootType_NonGenericType,
                    nameof(type));
            }

            if (BaseTypes.IsSchemaType(type)
                && !typeof(ObjectType).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_RootType_MustBeObjectType,
                    nameof(type));
            }

            var reference = TypeReference.Create(type, TypeContext.Output);
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
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Binding_Invalid,
                    nameof(binding));
            }

            if (!_bindingCompiler.CanHandle(binding))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Binding_CannotBeHandled,
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

        public ISchemaBuilder SetContextData(string key, object value)
        {
            _contextData[key] = value;
            return this;
        }

        public ISchemaBuilder SetContextData(string key, Func<object, object> update)
        {
            _contextData.TryGetValue(key, out var value);
            _contextData[key] = update(value);
            return this;
        }

        public ISchemaBuilder AddTypeInterceptor(Type interceptor)
        {
            if (interceptor is null)
            {
                throw new ArgumentNullException(nameof(interceptor));
            }

            if (!typeof(ITypeInitializationInterceptor).IsAssignableFrom(interceptor))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Interceptor_NotSuppported,
                    nameof(interceptor));
            }

            _interceptors.Add(interceptor);
            return this;
        }

        public ISchemaBuilder AddTypeInterceptor(ITypeInitializationInterceptor interceptor)
        {
            if (interceptor is null)
            {
                throw new ArgumentNullException(nameof(interceptor));
            }

            _interceptors.Add(interceptor);
            return this;
        }

        public ISchemaBuilder OnBeforeCreate(
            Action<IDescriptorContext> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _onBeforeCreate.Add(action);
            return this;
        }

        public static SchemaBuilder New() => new SchemaBuilder();
    }
}
