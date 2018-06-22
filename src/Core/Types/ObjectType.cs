using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly Dictionary<string, InterfaceType> _interfaceMap =
            new Dictionary<string, InterfaceType>();
        private readonly Dictionary<string, Field> _fieldMap =
            new Dictionary<string, Field>();
        private IsOfType _isOfType;
        private ImmutableList<TypeReference> _interfaces;
        private ObjectTypeBinding _typeBinding;
        private bool _completed;

        protected ObjectType()
        {
            Initialize(Configure);
        }

        public ObjectType(Action<IObjectTypeDescriptor> configure)
        {
            Initialize(configure);
        }

        public TypeKind Kind { get; } = TypeKind.Object;

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
            Initialize(descriptor);
        }

        private void Initialize(ObjectTypeDescriptor descriptor)
        {
            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "The type name must not be null or empty.");
            }

            InitializeFields(descriptor);

            _isOfType = descriptor.IsOfType;
            _interfaces = descriptor.Interfaces;

            SyntaxNode = descriptor.SyntaxNode;
            Name = descriptor.Name;
            Description = descriptor.Description;
            IsIntrospection = descriptor.IsIntrospection;
        }

        private void InitializeFields(ObjectTypeDescriptor descriptor)
        {
            List<FieldBinding> fieldBindings = new List<FieldBinding>();
            foreach (FieldDescriptor fieldDescriptor in descriptor
                .GetFieldDescriptors())
            {
                Field field = new Field(fieldDescriptor);
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
        }

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            if (!_completed)
            {
                if (_interfaces != null)
                {
                    foreach (TypeReference typeReference in _interfaces)
                    {
                        schemaContext.Types.RegisterType(typeReference);
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
        }

        void INeedsInitialization.CompleteType(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            if (!_completed)
            {
                foreach (Field field in _fieldMap.Values)
                {
                    field.CompleteField(schemaContext, reportError, this);
                }

                CompleteIsOfType();
                CompleteInterfaces(schemaContext.Types, reportError);

                _completed = true;
            }
        }

        private void CompleteIsOfType()
        {
            if (_isOfType == null)
            {
                if (_typeBinding?.Type == null)
                {
                    _isOfType = IsOfTypeNameBased;
                }
                else
                {
                    _isOfType = IsOfTypeWithNativeType;
                }
            }
        }

        private bool IsOfTypeWithNativeType(
            IResolverContext context,
            object result)
        {
            if (result == null)
            {
                return true;
            }
            return _typeBinding.Type.IsInstanceOfType(result);
        }

        private bool IsOfTypeNameBased(
            IResolverContext context,
            object result)
        {
            if (result == null)
            {
                return true;
            }
            return context.GetType().Name
                .Equals(Name, StringComparison.Ordinal);
        }

        private void CompleteInterfaces(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError)
        {
            if (_interfaces != null)
            {
                foreach (InterfaceType interfaceType in _interfaces
                    .Select(t => typeRegistry.GetType<InterfaceType>(t))
                    .Where(t => t != null))
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
                                    $"Object type {Name} does not implement " +
                                    $"all arguments of field {interfaceField.Name} " +
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
            new ObjectTypeDescriptor<T>();

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
