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
        private readonly Dictionary<ITypeReference, ITypeReference> _clrTypes =
            new Dictionary<ITypeReference, ITypeReference>();
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
                _schema = new ClrTypeReference(type, TypeContext.None);
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
                _types.Add(new ClrTypeReference(
                    type,
                    SchemaTypeReference.InferTypeContext(type)));
            }

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
            _clrTypes[new ClrTypeReference(clrType, context)] =
                new ClrTypeReference(schemaType, context);

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

        public ISchemaBuilder AddContextData(string key, object value)
        {
            _contextData.Add(key, value);
            return this;
        }

        public ISchemaBuilder SetContextData(string key, object value)
        {
            _contextData[key] = value;
            return this;
        }

        public ISchemaBuilder RemoveContextData(string key)
        {
            _contextData.Remove(key);
            return this;
        }

        public ISchemaBuilder ClearContextData()
        {
            _contextData.Clear();
            return this;
        }

        public static SchemaBuilder New() => new SchemaBuilder();
    }
}
