using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class ObjectType
        : TypeBase
        , IComplexOutputType
    {
        private readonly Dictionary<string, InterfaceType> _interfaceMap =
            new Dictionary<string, InterfaceType>();
        private IsOfType _isOfType;
        private List<TypeReference> _interfaces;
        private ObjectTypeBinding _typeBinding;
        private bool _completed;

        protected ObjectType()
            : base(TypeKind.Object)
        {
            Initialize(Configure);
        }

        public ObjectType(Action<IObjectTypeDescriptor> configure)
            : base(TypeKind.Object)
        {
            Initialize(configure);
        }

        public ObjectTypeDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyDictionary<string, InterfaceType> Interfaces => _interfaceMap;

        public FieldCollection<ObjectField> Fields { get; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public bool IsOfType(IResolverContext context, object resolverResult)
            => _isOfType(context, resolverResult);

        #region Configuration

        internal virtual ObjectTypeDescriptor CreateDescriptor() =>
            new ObjectTypeDescriptor(GetType());

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

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

            ObjectTypeDescription description = descriptor.CreateDescription();
            InitializeFields(description);

            _isOfType = description.IsOfType;
            _interfaces = description.Interfaces;

            SyntaxNode = description.SyntaxNode;
            Name = description.Name;
            Description = description.Description;
        }

        private void InitializeFields(ObjectTypeDescription description)
        {
            List<FieldBinding> fieldBindings = new List<FieldBinding>();
            List<ObjectField> fields = new List<ObjectField>();

            foreach (ObjectFieldDescription fieldDescription in description.Fields)
            {
                ObjectField field = new ObjectField(fieldDescription);
                fields.Add(field);

                if (fieldDescription.Member != null)
                {
                    fieldBindings.Add(new FieldBinding(
                        field.Name, fieldDescription.Member, field));
                }
            }

            if (description.NativeType != null)
            {
                _typeBinding = new ObjectTypeBinding(
                    description.Name, description.NativeType, 
                    this, fieldBindings);
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

                foreach (Field field in _fields.Values)
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
                foreach (Field field in _fields.Values)
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
