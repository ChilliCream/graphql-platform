using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Configuration;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Introspection;

namespace HotChocolate
{
    public partial class SchemaBuilder : ISchemaBuilder
    {
        private delegate ITypeReference CreateRef(ITypeInspector typeInspector);

        private readonly Dictionary<string, object> _contextData = new();
        private readonly List<FieldMiddleware> _globalComponents = new();
        private readonly List<LoadSchemaDocument> _documents = new();
        private readonly List<CreateRef> _types = new();
        private readonly List<Type> _resolverTypes = new();
        private readonly Dictionary<OperationType, CreateRef> _operations = new();
        private readonly Dictionary<FieldReference, FieldResolver> _resolvers = new();
        private readonly Dictionary<(Type, string), List<CreateConvention>> _conventions = new();
        private readonly Dictionary<Type, (CreateRef, CreateRef)> _clrTypes = new();
        private readonly List<object> _schemaInterceptors = new();
        private readonly List<object> _typeInterceptors = new()
        {
            typeof(IntrospectionTypeInterceptor),
            typeof(InterfaceCompletionTypeInterceptor),
            typeof(CostTypeInterceptor)
        };
        private readonly IBindingCompiler _bindingCompiler = new BindingCompiler();
        private SchemaOptions _options = new();
        private IsOfTypeFallback _isOfType;
        private IServiceProvider _services;
        private CreateRef _schema;

        public ISchemaBuilder SetSchema(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (typeof(Schema).IsAssignableFrom(type))
            {
                _schema = ti => ti.GetTypeRef(type);
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
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (schema is TypeSystemObjectBase)
            {
                _schema = _ => new SchemaTypeReference(schema);
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

            _schema = _ => new SchemaTypeReference(new Schema(configure));
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
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configure(_options);
            return this;
        }

        public ISchemaBuilder Use(FieldMiddleware middleware)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _globalComponents.Add(middleware);
            return this;
        }

        public ISchemaBuilder AddDocument(LoadSchemaDocument loadSchemaDocument)
        {
            if (loadSchemaDocument is null)
            {
                throw new ArgumentNullException(nameof(loadSchemaDocument));
            }

            _documents.Add(loadSchemaDocument);
            return this;
        }

        public ISchemaBuilder AddType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsDefined(typeof(GraphQLResolverOfAttribute), true))
            {
                AddResolverType(type);
            }
            else
            {
                _types.Add(ti => ti.GetTypeRef(type));
            }

            return this;
        }

        public ISchemaBuilder TryAddConvention(
            Type convention,
            CreateConvention factory,
            string scope = null)
        {
            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (!_conventions.ContainsKey((convention, scope)))
            {
                AddConvention(convention, factory, scope);
            }

            return this;
        }

        public ISchemaBuilder AddConvention(
            Type convention,
            CreateConvention factory,
            string scope = null)
        {
            if (convention is null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            if(!_conventions.TryGetValue((convention, scope), out List<CreateConvention> factories))
            {
                factories = new List<CreateConvention>();
                _conventions[(convention, scope)] = factories;
            }

            factories.Add(factory);

            return this;
        }

        public ISchemaBuilder BindClrType(Type runtimeType, Type schemaType)
        {
            if (runtimeType is null)
            {
                throw new ArgumentNullException(nameof(runtimeType));
            }

            if (schemaType is null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            if (!schemaType.IsSchemaType())
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_MustBeSchemaType,
                    nameof(schemaType));
            }

            TypeContext context = SchemaTypeReference.InferTypeContext(schemaType);
            _clrTypes[runtimeType] =
                (ti => ti.GetTypeRef(runtimeType, context),
                ti => ti.GetTypeRef(schemaType, context));

            return this;
        }

        private void AddResolverType(Type type)
        {
            GraphQLResolverOfAttribute attribute =
                type.GetCustomAttribute<GraphQLResolverOfAttribute>(true);

            _resolverTypes.Add(type);

            if (attribute?.Types != null)
            {
                foreach (Type schemaType in attribute.Types)
                {
                    if (typeof(ObjectType).IsAssignableFrom(schemaType) &&
                        schemaType.IsSchemaType())
                    {
                        _types.Add(ti => ti.GetTypeRef(schemaType));
                    }
                }
            }
        }

        public ISchemaBuilder AddType(INamedType type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _types.Add(_ => TypeReference.Create(type));
            return this;
        }

        public ISchemaBuilder AddType(INamedTypeExtension type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _types.Add(_ => TypeReference.Create(type));
            return this;
        }

        public ISchemaBuilder AddDirectiveType(DirectiveType type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _types.Add(_ => TypeReference.Create(type));
            return this;
        }

        public ISchemaBuilder AddRootType(
            Type type,
            OperationType operation)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsClass)
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_RootType_MustBeClass,
                    nameof(type));
            }

            if (type.IsNonGenericSchemaType())
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_RootType_NonGenericType,
                    nameof(type));
            }

            if (type.IsSchemaType()
                && !typeof(ObjectType).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_RootType_MustBeObjectType,
                    nameof(type));
            }

            if (_operations.ContainsKey(operation))
            {
                throw new ArgumentException(
                    string.Format(
                        TypeResources.SchemaBuilder_AddRootType_TypeAlreadyRegistered,
                        operation),
                    nameof(operation));
            }

            _operations.Add(operation, ti => ti.GetTypeRef(type, TypeContext.Output));
            _types.Add(ti => ti.GetTypeRef(type, TypeContext.Output));
            return this;
        }

        public ISchemaBuilder AddRootType(
            ObjectType type,
            OperationType operation)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_operations.ContainsKey(operation))
            {
                throw new ArgumentException(
                    string.Format(
                        TypeResources.SchemaBuilder_AddRootType_TypeAlreadyRegistered,
                        operation),
                    nameof(operation));
            }

            SchemaTypeReference reference = TypeReference.Create(type);
            _operations.Add(operation, _ => reference);
            _types.Add(_ => reference);
            return this;
        }

        public ISchemaBuilder AddResolver(FieldResolver fieldResolver)
        {
            if (fieldResolver is null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            _resolvers.Add(fieldResolver.ToFieldReference(), fieldResolver);
            return this;
        }

        public ISchemaBuilder AddBinding(IBindingInfo binding)
        {
            if (binding is null)
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
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = _services is null ? services : _services.Include(services);

            return this;
        }

        public ISchemaBuilder SetContextData(string key, object value)
        {
            _contextData[key] = value;
            return this;
        }

        public ISchemaBuilder SetContextData(string key, Func<object, object> update)
        {
            _contextData.TryGetValue(key, out object value);
            _contextData[key] = update(value);
            return this;
        }

        public ISchemaBuilder TryAddTypeInterceptor(Type interceptor)
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

            if (!_typeInterceptors.Contains(interceptor))
            {
                _typeInterceptors.Add(interceptor);
            }

            return this;
        }

        public ISchemaBuilder TryAddTypeInterceptor(ITypeInitializationInterceptor interceptor)
        {
            if (interceptor is null)
            {
                throw new ArgumentNullException(nameof(interceptor));
            }

            if (!_typeInterceptors.Contains(interceptor))
            {
                _typeInterceptors.Add(interceptor);
            }

            return this;
        }

        public ISchemaBuilder TryAddSchemaInterceptor(Type interceptor)
        {
            if (interceptor is null)
            {
                throw new ArgumentNullException(nameof(interceptor));
            }

            if (!typeof(ISchemaInterceptor).IsAssignableFrom(interceptor))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilder_Interceptor_NotSuppported,
                    nameof(interceptor));
            }

            if (!_schemaInterceptors.Contains(interceptor))
            {
                _schemaInterceptors.Add(interceptor);
            }

            return this;
        }

        public ISchemaBuilder TryAddSchemaInterceptor(ISchemaInterceptor interceptor)
        {
            if (interceptor is null)
            {
                throw new ArgumentNullException(nameof(interceptor));
            }

            if (!_schemaInterceptors.Contains(interceptor))
            {
                _schemaInterceptors.Add(interceptor);
            }

            return this;
        }

        public static SchemaBuilder New() => new();
    }
}
