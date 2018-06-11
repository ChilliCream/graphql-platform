using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class ObjectType
        : INamedType
        , IOutputType
        , INullableType
        , ITypeSystemNode
        , INeedsInitialization
        , IHasFields
    {
        private IsOfType _isOfType;
        private Func<ITypeRegistry, IEnumerable<InterfaceType>> _interfaceFactory;
        private IReadOnlyCollection<TypeInfo> _interfaceTypeInfos;
        private ObjectTypeBinding _typeBinding;
        private Dictionary<string, InterfaceType> _interfaceMap =
            new Dictionary<string, InterfaceType>();
        private Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();

        protected ObjectType()
        {
            Initialize(Configure);
        }

        public ObjectType(Action<IObjectTypeDescriptor> configure)
        {
            Initialize(configure);
        }

        internal ObjectType(ObjectTypeConfig config)
        {
            Initialize(config);
        }

        public ObjectTypeDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        internal bool IsIntrospection { get; private set; }

        public IReadOnlyDictionary<string, InterfaceType> Interfaces => _interfaceMap;

        public IReadOnlyDictionary<string, Field> Fields => _fieldMap;

        public bool IsOfType(IResolverContext context, object resolverResult)
            => _isOfType(context, resolverResult);

        #region Configuration

        internal virtual ObjectTypeDescriptor CreateDescriptor() =>
            new ObjectTypeDescriptor(GetType());

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        #endregion

        #region ITypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            foreach (InterfaceType node in Interfaces.Values)
            {
                yield return node;
            }

            foreach (Field node in Fields.Values)
            {
                yield return node;
            }
        }

        #endregion

        #region Initialization

        private void Initialize(Action<IObjectTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            ObjectTypeDescriptor descriptor = CreateDescriptor();
            configure(descriptor);

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "The type name must not be null or empty.");
            }

            List<FieldBinding> fieldBindings = new List<FieldBinding>();
            foreach (FieldDescriptor fieldDescriptor in descriptor.GetFieldDescriptors())
            {
                Field field = fieldDescriptor.CreateField();
                _fieldMap[fieldDescriptor.Name] = field;

                if (fieldDescriptor.Member != null)
                {
                    fieldBindings.Add(new FieldBinding(
                        fieldDescriptor.Name, fieldDescriptor.Member, field));
                }
            }

            if (descriptor.NativeType != null)
            {
                _typeBinding = new ObjectTypeBinding(
                    descriptor.Name, descriptor.NativeType, this, fieldBindings);
            }

            _isOfType = descriptor.IsOfType;
            _interfaceFactory = r => descriptor.Interfaces
                .Select(t => t.TypeFactory(r))
                .Cast<InterfaceType>();
            _interfaceTypeInfos = descriptor.Interfaces;

            Name = descriptor.Name;
            Description = descriptor.Description;
            IsIntrospection = descriptor.IsIntrospection;
        }

        private void Initialize(ObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An object type name must not be null or empty.",
                    nameof(config));
            }

            Field[] fields = config.Fields?.ToArray();
            if (fields?.Length == 0)
            {
                throw new ArgumentException(
                    $"The object type `{Name}` has no fields.",
                    nameof(config));
            }

            foreach (Field field in fields)
            {
                _fieldMap[field.Name] = field;
            }

            _isOfType = config.IsOfType;
            _interfaceFactory = config.Interfaces;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
            IsIntrospection = config.IsIntrospection;
        }

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            if (_interfaceTypeInfos != null)
            {
                foreach (TypeInfo interfaceTypeInfo in _interfaceTypeInfos)
                {
                    schemaContext.Types.RegisterType(interfaceTypeInfo.NativeNamedType);
                }
            }

            foreach (Field field in _fieldMap.Values)
            {
                field.RegisterDependencies(schemaContext, reportError, this);
            }

            if (_typeBinding != null)
            {
                schemaContext.Types.RegisterType(this, _typeBinding);
            }
        }

        void INeedsInitialization.CompleteType(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            foreach (Field field in _fieldMap.Values)
            {
                field.CompleteField(schemaContext, reportError, this);
            }

            if (_interfaceFactory != null)
            {
                foreach (InterfaceType interfaceType in
                    _interfaceFactory(schemaContext.Types))
                {
                    _interfaceMap[interfaceType.Name] = interfaceType;
                }

                CheckIfAllInterfaceFieldsAreImplemented(reportError);
            }
        }

        private void CheckIfAllInterfaceFieldsAreImplemented(
            Action<SchemaError> reportError)
        {
            foreach (InterfaceType interfaceType in _interfaceMap.Values)
            {
                foreach (Field interfaceField in interfaceType.Fields.Values)
                {
                    if (Fields.TryGetValue(interfaceField.Name, out Field field))
                    {
                        foreach (InputField interfaceArgument in interfaceField.Arguments.Values)
                        {
                            if (!field.Arguments.ContainsKey(
                                interfaceArgument.Name))
                            {
                                reportError(new SchemaError(
                                    $"Object type {Name} does not implement the " +
                                    $"field all arguments of field {interfaceField.Name} " +
                                    $"from interface {interfaceType.Name}.",
                                    this));
                            }
                        }
                    }
                    else
                    {
                        reportError(new SchemaError(
                            $"Object type {Name} does not implement the " +
                            $"field {interfaceField.Name} " +
                            $"from interface {interfaceType.Name}.",
                            this));
                    }
                }
            }
        }

        #endregion
    }

    public class ObjectType<T>
        : ObjectType
    {
        public ObjectType()
        {
        }

        public ObjectType(Action<IObjectTypeDescriptor<T>> configure)
            : base(d => configure((IObjectTypeDescriptor<T>)d))
        {
        }

        #region Configuration

        internal sealed override ObjectTypeDescriptor CreateDescriptor() =>
            new ObjectTypeDescriptor<T>(typeof(T));

        protected sealed override void Configure(IObjectTypeDescriptor descriptor)
        {
            Configure((IObjectTypeDescriptor<T>)descriptor);
        }

        protected virtual void Configure(IObjectTypeDescriptor<T> descriptor)
        {

        }

        #endregion
    }
}
