using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Types
{
    public class ObjectType
        : NamedTypeBase
        , ITypeSystemObject
        , IComplexOutputType
        , IHasClrType
    {
        private readonly Dictionary<NameString, InterfaceType> _interfaceMap =
            new Dictionary<NameString, InterfaceType>();
        private ObjectTypeDefinition _definition;
        private readonly Action<IObjectTypeDescriptor> _configure;


        private IsOfType _isOfType;
        private List<TypeReference> _interfaces;
        private ObjectTypeBinding _typeBinding;

        protected ObjectType()
            : base(TypeKind.Object)
        {
            _configure = Configure;
        }

        public ObjectType(Action<IObjectTypeDescriptor> configure)
            : base(TypeKind.Object)
        {
            _configure = configure;
        }

        protected ObjectTypeDefinition Definition
        {
            get => _definition;
            set
            {
                if (_definition != null)
                {
                    // TODO : resources
                    throw new NotSupportedException(
                        "It is not allowed to change the type definition " +
                        "once it is set.");
                }

                _definition = value
                    ?? throw new ArgumentNullException(nameof(value));
            }
        }
        public ObjectTypeDefinitionNode SyntaxNode { get; private set; }

        public Type ClrType { get; protected set; }

        public IReadOnlyDictionary<NameString, InterfaceType> Interfaces =>
            _interfaceMap;

        public FieldCollection<ObjectField> Fields { get; private set; }

        IFieldCollection<IOutputField> IComplexOutputType.Fields => Fields;

        public bool IsOfType(IResolverContext context, object resolverResult)
            => _isOfType(context, resolverResult);

        void ITypeSystemObject.Initialize(IInitializationContext context) =>
            OnInitialize(context);

        protected virtual void OnInitialize(IInitializationContext context)
        {
            ObjectTypeDescriptor descriptor = ObjectTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
                GetType());
            _configure(descriptor);
            Definition = descriptor.CreateDefinition();
        }

        protected void RegisterDependencies(
            IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            context.RegisterDependencyRange(
                definition.Interfaces,
                TypeDependencyKind.Default);

            Fields = new FieldCollection<ObjectField>(
                definition.Fields.Select(t => new ObjectField(t)));

            foreach ()
        }



        public static bool TryCreateFieldReference(
            ObjectTypeDefinition typeDefinition,
            ObjectFieldDefinition fieldDefinition,
            out IFieldReference fieldReference)
        {
            if (fieldDefinition.Resolver != null)
            {
                fieldReference = new FieldResolver(
                    typeDefinition.Name,
                    fieldDefinition.Name,
                    fieldDefinition.Resolver);
                return true;
            }

            if (fieldDefinition.Member != null)
            {
                // ? resolver type
                fieldReference = new FieldMember(
                    typeDefinition.Name,
                    fieldDefinition.Name,
                    fieldDefinition.Member);
                return true;
            }

            fieldReference = null;
            return false;
        }


        void ITypeSystemObject.CompleteName(ICompletionContext context)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnCompleteName(ICompletionContext context)
        {

        }

        void ITypeSystemObject.CompleteObject(ICompletionContext context)
        {
            throw new NotImplementedException();
        }

        protected void OnCompleteObject(ICompletionContext context)
        {
            throw new NotImplementedException();
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }


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
            ICollection<FieldBinding> fieldBindings,
            ICollection<ObjectField> fields)
        {
            foreach (ObjectFieldDescription fieldDescription in
                fieldDescriptions)
            {
                var field = new ObjectField(fieldDescription);
                fields.Add(field);

                if (fieldDescription.ResolverType == null
                    && fieldDescription.ClrMember != null)
                {
                    fieldBindings.Add(new FieldBinding(
                        field.Name, fieldDescription.ClrMember, field));
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

            CompleteClrType(context);
            CompleteIsOfType(context);
            CompleteInterfaces(context);
            CompleteFields(context);

            ValidateInterfaceImplementation(context);
        }

        private void CompleteClrType(
            ITypeInitializationContext context)
        {
            if (ClrType == null
                && context.TryGetNativeType(this, out Type clrType))
            {
                ClrType = clrType;
            }

            if (ClrType == null)
            {
                ClrType = typeof(object);
            }
        }

        private void CompleteIsOfType(ITypeInitializationContext context)
        {
            if (_isOfType == null)
            {
                if (context.IsOfType != null)
                {
                    IsOfTypeFallback isOfType = context.IsOfType;
                    _isOfType = (ctx, obj) => isOfType(this, ctx, obj);
                }
                else if (ClrType == typeof(object))
                {
                    _isOfType = IsOfTypeWithName;
                }
                else
                {
                    _isOfType = IsOfTypeWithClrType;
                }
            }
        }

        private bool IsOfTypeWithClrType(
            IResolverContext context,
            object result)
        {
            if (result == null)
            {
                return true;
            }
            return ClrType.IsInstanceOfType(result);
        }

        private bool IsOfTypeWithName(
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
            if (ClrType != typeof(object))
            {
                Type[] possibleInterfaceTypes = ClrType.GetInterfaces();
                for (int i = 0; i < possibleInterfaceTypes.Length; i++)
                {
                    InterfaceType type = context.GetType<InterfaceType>(
                        new TypeReference(
                            possibleInterfaceTypes[i],
                            TypeContext.Output));
                    if (type != null)
                    {
                        _interfaceMap[type.Name] = type;
                    }
                }
            }

            foreach (InterfaceType interfaceType in _interfaces
                .Select(t => context.GetType<InterfaceType>(t))
                .Where(t => t != null))
            {
                if (!_interfaceMap.ContainsKey(interfaceType.Name))
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
                foreach (IGrouping<NameString, InterfaceField> fieldGroup in
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
            IGrouping<NameString, InterfaceField> interfaceField)
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
            IGrouping<NameString, InterfaceField> interfaceField)
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
}
