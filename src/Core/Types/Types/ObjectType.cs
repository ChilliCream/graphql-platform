using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Types
{
    public class ObjectType
        : NamedTypeBase
        , IComplexOutputType
    {
        private readonly Dictionary<string, InterfaceType> _interfaceMap =
            new Dictionary<string, InterfaceType>();
        private ObjectTypeDescription _description;
        private IsOfType _isOfType;
        private List<TypeReference> _interfaces;
        private ObjectTypeBinding _typeBinding;

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

        public IReadOnlyDictionary<string, InterfaceType> Interfaces =>
            _interfaceMap;

        public FieldCollection<ObjectField> Fields { get; private set; }

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

            _description = descriptor.CreateDescription();

            _isOfType = _description.IsOfType;
            _interfaces = _description.Interfaces;

            SyntaxNode = _description.SyntaxNode;

            Initialize(_description.Name, _description.Description,
                new DirectiveCollection(
                    this,
                    DirectiveLocation.Object,
                    _description.Directives));
        }

        private void InitializeFields(ObjectTypeDescription description)
        {
            var fieldBindings = new List<FieldBinding>();
            var fields = new List<ObjectField> { new __TypeNameField() };

            CreateFieldsAndBindings(description.Fields, fieldBindings, fields);

            if (description.ClrType != null)
            {
                _typeBinding = new ObjectTypeBinding(
                    description.Name, description.ClrType,
                    this, fieldBindings);
            }

            Fields = new FieldCollection<ObjectField>(fields);
        }

        private void CreateFieldsAndBindings(
            IEnumerable<ObjectFieldDescription> fieldDescriptions,
            List<FieldBinding> fieldBindings,
            List<ObjectField> fields)
        {
            foreach (ObjectFieldDescription fieldDescription in
                fieldDescriptions)
            {
                var field = new ObjectField(fieldDescription);
                fields.Add(field);

                if (fieldDescription.ResolverType == null
                    && fieldDescription.Member != null)
                {
                    fieldBindings.Add(new FieldBinding(
                        field.Name, fieldDescription.Member, field));
                }
            }
        }

        private void AddLateBoundResolverFields(IEnumerable<Type> resolverTypes)
        {
            if (resolverTypes.Any())
            {
                Dictionary<string, ObjectFieldDescription> descriptions =
                    _description.Fields.ToDictionary(t => t.Name);
                var processed = new HashSet<string>();

                foreach (Type resolverType in resolverTypes)
                {
                    ObjectTypeDescriptor.AddResolverType(
                        descriptions,
                        processed,
                        _description.ClrType ?? typeof(object),
                        resolverType);
                }

                _description.Fields.Clear();
                _description.Fields.AddRange(descriptions.Values);
            }
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            AddLateBoundResolverFields(context.GetResolverTypes(Name));
            InitializeFields(_description);

            base.OnRegisterDependencies(context);

            if (_interfaces != null)
            {
                foreach (TypeReference typeReference in _interfaces)
                {
                    context.RegisterType(typeReference);
                }
            }

            RegisterFields(context);

            if (_typeBinding != null)
            {
                context.RegisterType(this, _typeBinding);
            }
        }

        private void RegisterFields(ITypeInitializationContext context)
        {
            if (context.IsQueryType)
            {
                var fields = new List<ObjectField>
                {
                    new __TypeField(),
                    new __SchemaField()
                };
                fields.AddRange(Fields);
                Fields = new FieldCollection<ObjectField>(fields);
            }

            foreach (INeedsInitialization field in
                Fields.Cast<INeedsInitialization>())
            {
                field.RegisterDependencies(context);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            CompleteIsOfType();
            CompleteInterfaces(context);
            CompleteFields(context);

            ValidateInterfaceImplementation(context);
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

            Type type = result.GetType();
            return type.Name.Equals(Name, StringComparison.Ordinal);
        }

        private void CompleteInterfaces(
            ITypeInitializationContext context)
        {
            if (_interfaces != null)
            {
                foreach (InterfaceType interfaceType in _interfaces
                    .Select(t => context.GetType<InterfaceType>(t))
                    .Where(t => t != null))
                {
                    _interfaceMap[interfaceType.Name] = interfaceType;
                }
            }
        }

        private void ValidateInterfaceImplementation(
            ITypeInitializationContext context)
        {
            if (_interfaceMap.Count > 0)
            {
                foreach (IGrouping<string, InterfaceField> fieldGroup in
                    _interfaceMap.Values
                        .SelectMany(t => t.Fields)
                        .GroupBy(t => t.Name))
                {
                    ValidateField(context, fieldGroup);
                }
            }
        }

        private void ValidateField(
            ITypeInitializationContext context,
            IGrouping<string, InterfaceField> interfaceField)
        {
            InterfaceField first = interfaceField.First();
            if (ValidateInterfaceFieldGroup(context, first, interfaceField))
            {
                ValidateObjectField(context, first);
            }
        }

        private bool ValidateInterfaceFieldGroup(
            ITypeInitializationContext context,
            InterfaceField first,
            IGrouping<string, InterfaceField> interfaceField)
        {
            if (interfaceField.Count() == 1)
            {
                return true;
            }

            foreach (InterfaceField field in interfaceField)
            {
                if (!field.Type.IsEqualTo(first.Type))
                {
                    context.ReportError(new SchemaError(
                        "The return type of the interface field " +
                        $"{first.Name} from interface " +
                        $"{first.DeclaringType.Name} and " +
                        $"{field.DeclaringType.Name} do not match " +
                        $"and are implemented by object type {Name}.",
                        this));
                    return false;
                }

                if (!ArgumentsAreEqual(field.Arguments, first.Arguments))
                {
                    context.ReportError(new SchemaError(
                        $"The arguments of the interface field {first.Name} " +
                        $"from interface {first.DeclaringType.Name} and " +
                        $"{field.DeclaringType.Name} do not match " +
                        $"and are implemented by object type {Name}.",
                        this));
                    return false;
                }
            }

            return true;
        }

        private void ValidateObjectField(
            ITypeInitializationContext context,
            InterfaceField first)
        {
            if (Fields.TryGetField(first.Name, out ObjectField field))
            {
                if (!field.Type.IsEqualTo(first.Type))
                {
                    context.ReportError(new SchemaError(
                        "The return type of the interface field " +
                        $"{first.Name} does not match the field declared " +
                        $"by object type {Name}.",
                        this));
                }

                if (!ArgumentsAreEqual(field.Arguments, first.Arguments))
                {
                    context.ReportError(new SchemaError(
                        $"Object type {Name} does not implement " +
                        $"all arguments of field {first.Name} " +
                        $"from interface {first.DeclaringType.Name}.",
                        this));
                }
            }
            else
            {
                context.ReportError(new SchemaError(
                    $"Object type {Name} does not implement the " +
                    $"field {first.Name} " +
                    $"from interface {first.DeclaringType.Name}.",
                    this));
            }
        }

        private bool ArgumentsAreEqual(
            FieldCollection<InputField> x,
            FieldCollection<InputField> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            foreach (InputField xfield in x)
            {
                if (!y.TryGetField(xfield.Name, out InputField yfield)
                    || !xfield.Type.IsEqualTo(yfield.Type))
                {
                    return false;
                }
            }

            return true;
        }

        private void CompleteFields(ITypeInitializationContext context)
        {
            foreach (INeedsInitialization field in
                Fields.Cast<INeedsInitialization>())
            {
                field.CompleteType(context);
            }
        }

        #endregion
    }

    public class ObjectType<T>
        : ObjectType
        , IHasClrType
    {
        public ObjectType()
        {
        }

        public ObjectType(Action<IObjectTypeDescriptor<T>> configure)
            : base(d => configure((IObjectTypeDescriptor<T>)d))
        {
        }

        public Type ClrType { get; } = typeof(T);


        #region Configuration

        internal sealed override ObjectTypeDescriptor CreateDescriptor() =>
            new ObjectTypeDescriptor<T>();

        protected sealed override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            Configure((IObjectTypeDescriptor<T>)descriptor);
        }

        protected virtual void Configure(IObjectTypeDescriptor<T> descriptor)
        {

        }

        #endregion
    }
}
