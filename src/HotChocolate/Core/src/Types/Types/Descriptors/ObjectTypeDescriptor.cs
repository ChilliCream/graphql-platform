using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.FieldBindingFlags;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class ObjectTypeDescriptor
    : DescriptorBase<ObjectTypeConfiguration>
    , IObjectTypeDescriptor
{
    private readonly List<ObjectFieldDescriptor> _fields = [];

    protected ObjectTypeDescriptor(IDescriptorContext context, Type clrType)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(clrType);

        Configuration.RuntimeType = clrType;
        Configuration.Name = context.Naming.GetTypeName(clrType, TypeKind.Object);
        Configuration.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Object);
    }

    protected ObjectTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
        Configuration.RuntimeType = typeof(object);
    }

    protected ObjectTypeDescriptor(
        IDescriptorContext context,
        ObjectTypeConfiguration definition)
        : base(context)
    {
        Configuration = definition ?? throw new ArgumentNullException(nameof(definition));

        foreach (var field in definition.Fields)
        {
            _fields.Add(ObjectFieldDescriptor.From(Context, field));
        }
    }

    protected internal override ObjectTypeConfiguration Configuration { get; protected set; } = new();

    protected ICollection<ObjectFieldDescriptor> Fields => _fields;

    protected override void OnCreateConfiguration(
        ObjectTypeConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (Configuration is { AttributesAreApplied: false, FieldBindingType: not null })
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Configuration.FieldBindingType);

            if (Configuration.AttributeBindingTypes.Length > 0)
            {
                foreach (var type in Configuration.AttributeBindingTypes)
                {
                    Context.TypeInspector.ApplyAttributes(
                        Context,
                        this,
                        type);
                }
            }

            Configuration.AttributesAreApplied = true;
        }

        foreach (var field in _fields)
        {
            if (field.Configuration.Ignore)
            {
                // if this definition is used for a type extension we need a
                // binding to a field which shall be ignored. In case this is a
                // definition for the type it will be ignored by the type initialization.
                Configuration.FieldIgnores.Add(
                    new ObjectFieldBinding(field.Configuration.Name, ObjectFieldBindingType.Field));
            }
        }

        var fields = TypeMemHelper.RentObjectFieldConfigurationMap();
        var handledMembers = TypeMemHelper.RentMemberSet();

        foreach (var fieldDescriptor in _fields)
        {
            var fieldDefinition = fieldDescriptor.CreateConfiguration();

            if (!fieldDefinition.Ignore && !string.IsNullOrEmpty(fieldDefinition.Name))
            {
                fields[fieldDefinition.Name] = fieldDefinition;
            }

            if (fieldDefinition.Member is { } member)
            {
                handledMembers.Add(member);
            }
        }

        OnCompleteFields(fields, handledMembers);

        // if we find fields that match field name that are ignored we will
        // remove them from the field map.
        foreach (var ignore in Configuration.GetFieldIgnores())
        {
            fields.Remove(ignore.Name);
        }

        Configuration.Fields.Clear();
        Configuration.Fields.AddRange(fields.Values);

        TypeMemHelper.Return(fields);
        TypeMemHelper.Return(handledMembers);

        base.OnCreateConfiguration(definition);

        Context.Descriptors.Pop();
    }

    internal void InferFieldsFromFieldBindingType()
    {
        var fields = TypeMemHelper.RentObjectFieldConfigurationMap();
        var handledMembers = TypeMemHelper.RentMemberSet();

        InferFieldsFromFieldBindingType(fields, handledMembers, false);

        TypeMemHelper.Return(fields);
        TypeMemHelper.Return(handledMembers);
    }

    private protected void InferFieldsFromFieldBindingType(
        IDictionary<string, ObjectFieldConfiguration> fields,
        ISet<MemberInfo> handledMembers,
        bool createDefinition = true)
    {
        var skip = false;
        HashSet<string>? subscribeRes = null;
        Dictionary<MemberInfo, string>? subscribeResLook = null;

        if (Configuration.Fields.IsImplicitBinding()
            && Configuration.FieldBindingType is not null)
        {
            var inspector = Context.TypeInspector;
            var naming = Context.Naming;
            var type = Configuration.FieldBindingType;
            var isExtension = Configuration.IsExtension;
            var includeStatic = (Configuration.FieldBindingFlags & Static) == Static;
            var members = inspector.GetMembers(type, isExtension, includeStatic);

            foreach (var member in members)
            {
                var name = naming.GetMemberName(member, MemberKind.ObjectField);

                if (handledMembers.Add(member)
                    && !fields.ContainsKey(name)
                    && IncludeField(ref skip, ref subscribeRes, ref subscribeResLook, members, member))
                {
                    var descriptor = ObjectFieldDescriptor.New(
                        Context,
                        member,
                        Configuration.RuntimeType,
                        type);

                    if (subscribeResLook is not null
                        && subscribeResLook.TryGetValue(member, out var with))
                    {
                        descriptor.Configuration.SubscribeWith = with;
                    }

                    if (isExtension && inspector.IsMemberIgnored(member))
                    {
                        descriptor.Ignore();
                    }

                    _fields.Add(descriptor);
                    handledMembers.Add(member);

                    if (createDefinition)
                    {
                        // the create definition call will trigger the OnCompleteField call
                        // on the field description and trigger the initialization of the
                        // fields arguments.
                        fields[name] = descriptor.CreateConfiguration();
                    }
                }
            }
        }

        static bool IncludeField(
            ref bool skip,
            ref HashSet<string>? subscribeResolver,
            ref Dictionary<MemberInfo, string>? subscribeResolverLookup,
            ReadOnlySpan<MemberInfo> allMembers,
            MemberInfo current)
        {
            // if there is now with declared we can include all members.
            if (skip)
            {
                return true;
            }

            if (subscribeResolver is null)
            {
                foreach (var member in allMembers)
                {
                    if (member.IsDefined(typeof(SubscribeAttribute))
                        && member.GetCustomAttribute<SubscribeAttribute>() is { With: not null } a)
                    {
                        subscribeResolver ??= [];
                        subscribeResolverLookup ??= [];
                        subscribeResolver.Add(a.With);
                        subscribeResolverLookup.Add(member, a.With);
                    }
                }

                skip = subscribeResolver is null;
            }

            return !subscribeResolver?.Contains(current.Name) ?? true;
        }
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, ObjectFieldConfiguration> fields,
        ISet<MemberInfo> handledMembers)
    { }

    public IObjectTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public IObjectTypeDescriptor Description(string? value)
    {
        Configuration.Description = value;
        return this;
    }

    public IObjectTypeDescriptor Implements<T>()
        where T : InterfaceType
    {
        if (typeof(T) == typeof(InterfaceType))
        {
            throw new ArgumentException(
                ObjectTypeDescriptor_InterfaceBaseClass);
        }

        Configuration.Interfaces.Add(
            Context.TypeInspector.GetTypeRef(typeof(T)));
        return this;
    }

    public IObjectTypeDescriptor Implements<T>(T type)
        where T : InterfaceType
    {
        ArgumentNullException.ThrowIfNull(type);

        Configuration.Interfaces.Add(new SchemaTypeReference(type));

        return this;
    }

    public IObjectTypeDescriptor Implements(NamedTypeNode type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Configuration.Interfaces.Add(TypeReference.Create(type, TypeContext.Output));
        return this;
    }

    public IObjectTypeDescriptor IsOfType(IsOfType? isOfType)
    {
        Configuration.IsOfType = isOfType ?? throw new ArgumentNullException(nameof(isOfType));
        return this;
    }

    public IObjectFieldDescriptor Field(string name)
    {
        var fieldDescriptor = _fields.Find(t => t.Configuration.Name.EqualsOrdinal(name));

        if (fieldDescriptor is not null)
        {
            return fieldDescriptor;
        }

        fieldDescriptor = ObjectFieldDescriptor.New(Context, name);
        _fields.Add(fieldDescriptor);
        return fieldDescriptor;
    }

    public IObjectFieldDescriptor Field<TResolver>(
        Expression<Func<TResolver, object?>> propertyOrMethod) =>
        Field<TResolver, object?>(propertyOrMethod);

    public IObjectFieldDescriptor Field(
        MemberInfo propertyOrMethod)
    {
        ArgumentNullException.ThrowIfNull(propertyOrMethod);

        if (propertyOrMethod is PropertyInfo || propertyOrMethod is MethodInfo)
        {
            var fieldDescriptor = _fields.Find(t => t.Configuration.Member == propertyOrMethod);

            if (fieldDescriptor is not null)
            {
                return fieldDescriptor;
            }

            fieldDescriptor = ObjectFieldDescriptor.New(
                Context,
                propertyOrMethod,
                Configuration.RuntimeType,
                propertyOrMethod.ReflectedType ?? Configuration.RuntimeType);
            _fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        throw new ArgumentException(
            ObjectTypeDescriptor_MustBePropertyOrMethod,
            nameof(propertyOrMethod));
    }

    public IObjectFieldDescriptor Field<TResolver, TPropertyType>(
        Expression<Func<TResolver, TPropertyType>> propertyOrMethod)
    {
        ArgumentNullException.ThrowIfNull(propertyOrMethod);

        var member = propertyOrMethod.TryExtractMember();

        if (member is PropertyInfo or MethodInfo)
        {
            var fieldDescriptor = _fields.Find(t => t.Configuration.Member == member);

            if (fieldDescriptor is not null)
            {
                return fieldDescriptor;
            }

            fieldDescriptor = ObjectFieldDescriptor.New(
                Context,
                member,
                Configuration.RuntimeType,
                typeof(TResolver));
            _fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        if (member is null)
        {
            var fieldDescriptor = ObjectFieldDescriptor.New(
                Context,
                propertyOrMethod,
                Configuration.RuntimeType,
                typeof(TResolver));
            _fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        throw new ArgumentException(
            ObjectTypeDescriptor_MustBePropertyOrMethod,
            nameof(member));
    }

    public IObjectTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Configuration.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IObjectTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Configuration.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IObjectTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
        return this;
    }

    public IObjectTypeDescriptor ExtendsType(Type extendsType)
    {
        Configuration.ExtendsType = extendsType;
        return this;
    }

    public IObjectTypeDescriptor ExtendsType<T>()
    {
        Configuration.ExtendsType = typeof(T);
        return this;
    }

    public static ObjectTypeDescriptor New(
        IDescriptorContext context) =>
        new(context);

    public static ObjectTypeDescriptor New(
        IDescriptorContext context,
        Type clrType) =>
        new(context, clrType);

    public static ObjectTypeDescriptor<T> New<T>(
        IDescriptorContext context) =>
        new(context);

    public static ObjectTypeExtensionDescriptor<T> NewExtension<T>(
        IDescriptorContext context) =>
        new(context);

    public static ObjectTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType) =>
        new(context, schemaType) { Configuration = { RuntimeType = typeof(object) } };

    public static ObjectTypeDescriptor From(
        IDescriptorContext context,
        ObjectTypeConfiguration definition) =>
        new(context, definition);

    public static ObjectTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        ObjectTypeConfiguration definition) =>
        new(context, definition);
}
