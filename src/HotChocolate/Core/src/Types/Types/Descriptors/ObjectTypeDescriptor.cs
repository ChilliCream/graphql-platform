using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.FieldBindingFlags;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class ObjectTypeDescriptor
    : DescriptorBase<ObjectTypeDefinition>
    , IObjectTypeDescriptor
{
    private readonly List<ObjectFieldDescriptor> _fields = [];

    protected ObjectTypeDescriptor(IDescriptorContext context, Type clrType)
        : base(context)
    {
        if (clrType is null)
        {
            throw new ArgumentNullException(nameof(clrType));
        }

        Definition.RuntimeType = clrType;
        Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Object);
        Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Object);
    }

    protected ObjectTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
        Definition.RuntimeType = typeof(object);
    }

    protected ObjectTypeDescriptor(
        IDescriptorContext context,
        ObjectTypeDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));

        foreach (var field in definition.Fields)
        {
            _fields.Add(ObjectFieldDescriptor.From(Context, field));
        }
    }

    protected internal override ObjectTypeDefinition Definition { get; protected set; } = new();

    protected ICollection<ObjectFieldDescriptor> Fields => _fields;

    protected override void OnCreateDefinition(
        ObjectTypeDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (Definition is { AttributesAreApplied: false, FieldBindingType: not null, })
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.FieldBindingType);

            if (Definition.AttributeBindingTypes.Length > 0)
            {
                foreach (var type in Definition.AttributeBindingTypes)
                {
                    Context.TypeInspector.ApplyAttributes(
                        Context,
                        this,
                        type);
                }
            }

            Definition.AttributesAreApplied = true;
        }

        foreach (var field in _fields)
        {
            if (field.Definition.Ignore)
            {
                // if this definition is used for a type extension we need a
                // binding to a field which shall be ignored. In case this is a
                // definition for the type it will be ignored by the type initialization.
                Definition.FieldIgnores.Add(
                    new ObjectFieldBinding(field.Definition.Name, ObjectFieldBindingType.Field));
            }
        }

        var fields = TypeMemHelper.RentObjectFieldDefinitionMap();
        var handledMembers = TypeMemHelper.RentMemberSet();

        foreach (var fieldDescriptor in _fields)
        {
            var fieldDefinition = fieldDescriptor.CreateDefinition();

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
        foreach (var ignore in Definition.GetFieldIgnores())
        {
            fields.Remove(ignore.Name);
        }

        Definition.Fields.Clear();
        Definition.Fields.AddRange(fields.Values);

        TypeMemHelper.Return(fields);
        TypeMemHelper.Return(handledMembers);

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    internal void InferFieldsFromFieldBindingType()
    {
        var fields = TypeMemHelper.RentObjectFieldDefinitionMap();
        var handledMembers = TypeMemHelper.RentMemberSet();

        InferFieldsFromFieldBindingType(fields, handledMembers, false);

        TypeMemHelper.Return(fields);
        TypeMemHelper.Return(handledMembers);
    }

    private protected void InferFieldsFromFieldBindingType(
        IDictionary<string, ObjectFieldDefinition> fields,
        ISet<MemberInfo> handledMembers,
        bool createDefinition = true)
    {
        var skip = false;
        HashSet<string>? subscribeRes = null;
        Dictionary<MemberInfo, string>? subscribeResLook = null;

        if (Definition.Fields.IsImplicitBinding() &&
            Definition.FieldBindingType is not null)
        {
            var inspector = Context.TypeInspector;
            var naming = Context.Naming;
            var type = Definition.FieldBindingType;
            var isExtension = Definition.IsExtension;
            var includeStatic = (Definition.FieldBindingFlags & Static) == Static;
            var members = inspector.GetMembers(type, isExtension, includeStatic);

            foreach (var member in members)
            {
                var name = naming.GetMemberName(member, MemberKind.ObjectField);

                if (handledMembers.Add(member) &&
                    !fields.ContainsKey(name) &&
                    IncludeField(ref skip, ref subscribeRes, ref subscribeResLook, members, member))
                {
                    var descriptor = ObjectFieldDescriptor.New(
                        Context,
                        member,
                        Definition.RuntimeType,
                        type);

                    if (subscribeResLook is not null &&
                        subscribeResLook.TryGetValue(member, out var with))
                    {
                        descriptor.Definition.SubscribeWith = with;
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
                        fields[name] = descriptor.CreateDefinition();
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
                    if (member.IsDefined(typeof(SubscribeAttribute)) &&
                        member.GetCustomAttribute<SubscribeAttribute>() is { With: not null, } a)
                    {
                        subscribeResolver ??= [];
                        subscribeResolverLookup ??= new Dictionary<MemberInfo, string>();
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
        IDictionary<string, ObjectFieldDefinition> fields,
        ISet<MemberInfo> handledMembers)
    { }

    public IObjectTypeDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public IObjectTypeDescriptor Description(string? value)
    {
        Definition.Description = value;
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

        Definition.Interfaces.Add(
            Context.TypeInspector.GetTypeRef(typeof(T)));
        return this;
    }

    public IObjectTypeDescriptor Implements<T>(T type)
        where T : InterfaceType
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        Definition.Interfaces.Add(new SchemaTypeReference(type));

        return this;
    }

    public IObjectTypeDescriptor Implements(NamedTypeNode type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        Definition.Interfaces.Add(TypeReference.Create(type, TypeContext.Output));
        return this;
    }

    public IObjectTypeDescriptor IsOfType(IsOfType? isOfType)
    {
        Definition.IsOfType = isOfType ?? throw new ArgumentNullException(nameof(isOfType));
        return this;
    }

    public IObjectFieldDescriptor Field(string name)
    {
        var fieldDescriptor = _fields.Find(t => t.Definition.Name.EqualsOrdinal(name));

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
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        if (propertyOrMethod is PropertyInfo || propertyOrMethod is MethodInfo)
        {
            var fieldDescriptor = _fields.Find(t => t.Definition.Member == propertyOrMethod);

            if (fieldDescriptor is not null)
            {
                return fieldDescriptor;
            }

            fieldDescriptor = ObjectFieldDescriptor.New(
                Context,
                propertyOrMethod,
                Definition.RuntimeType,
                propertyOrMethod.ReflectedType ?? Definition.RuntimeType);
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
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        var member = propertyOrMethod.TryExtractMember();

        if (member is PropertyInfo or MethodInfo)
        {
            var fieldDescriptor = _fields.Find(t => t.Definition.Member == member);

            if (fieldDescriptor is not null)
            {
                return fieldDescriptor;
            }

            fieldDescriptor = ObjectFieldDescriptor.New(
                Context,
                member,
                Definition.RuntimeType,
                typeof(TResolver));
            _fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        if (member is null)
        {
            var fieldDescriptor = ObjectFieldDescriptor.New(
                Context,
                propertyOrMethod,
                Definition.RuntimeType,
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
        Definition.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IObjectTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Definition.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IObjectTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
        return this;
    }

    public IObjectTypeDescriptor ExtendsType(Type extendsType)
    {
        Definition.ExtendsType = extendsType;
        return this;
    }

    public IObjectTypeDescriptor ExtendsType<T>()
    {
        Definition.ExtendsType = typeof(T);
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
        new(context, schemaType) { Definition = { RuntimeType = typeof(object), }, };

    public static ObjectTypeDescriptor From(
        IDescriptorContext context,
        ObjectTypeDefinition definition) =>
        new(context, definition);

    public static ObjectTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        ObjectTypeDefinition definition) =>
        new(context, definition);
}
