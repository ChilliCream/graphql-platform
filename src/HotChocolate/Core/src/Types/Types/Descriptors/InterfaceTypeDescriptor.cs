using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors;

public class InterfaceTypeDescriptor
    : DescriptorBase<InterfaceTypeDefinition>
    , IInterfaceTypeDescriptor
{
    protected InterfaceTypeDescriptor(
        IDescriptorContext context,
        Type clrType)
        : base(context)
    {
        if (clrType is null)
        {
            throw new ArgumentNullException(nameof(clrType));
        }

        Definition.RuntimeType = clrType;
        Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Interface);
        Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Interface);
    }

    protected InterfaceTypeDescriptor(
        IDescriptorContext context)
        : base(context)
    {
        Definition.RuntimeType = typeof(object);
    }

    protected InterfaceTypeDescriptor(
        IDescriptorContext context,
        InterfaceTypeDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));

        foreach (var field in definition.Fields)
        {
            Fields.Add(InterfaceFieldDescriptor.From(Context, field));
        }
    }

    protected internal override InterfaceTypeDefinition Definition { get; protected set; } =
        new();

    protected ICollection<InterfaceFieldDescriptor> Fields { get; } =
        new List<InterfaceFieldDescriptor>();

    protected override void OnCreateDefinition(
        InterfaceTypeDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (!Definition.AttributesAreApplied && Definition.RuntimeType != typeof(object))
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.RuntimeType);
            Definition.AttributesAreApplied = true;
        }

        var fields = new Dictionary<string, InterfaceFieldDefinition>();
        var handledMembers = new HashSet<MemberInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Fields.Select(t => t.CreateDefinition()),
            f => f.Member,
            fields,
            handledMembers);

        OnCompleteFields(fields, handledMembers);

        Definition.Fields.Clear();
        Definition.Fields.AddRange(fields.Values);

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, InterfaceFieldDefinition> fields,
        ISet<MemberInfo> handledMembers)
    {
    }

    public IInterfaceTypeDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public IInterfaceTypeDescriptor Description(string value)
    {
        Definition.Description = value;
        return this;
    }

    public IInterfaceTypeDescriptor Implements<T>()
        where T : InterfaceType
    {
        if (typeof(T) == typeof(InterfaceType))
        {
            throw new ArgumentException(
                TypeResources.InterfaceTypeDescriptor_InterfaceBaseClass);
        }

        Definition.Interfaces.Add(
            Context.TypeInspector.GetTypeRef(typeof(T), TypeContext.Output));
        return this;
    }

    public IInterfaceTypeDescriptor Implements<T>(T type)
        where T : InterfaceType
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        Definition.Interfaces.Add(new SchemaTypeReference(type));
        return this;
    }

    public IInterfaceTypeDescriptor Implements(NamedTypeNode type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        Definition.Interfaces.Add(TypeReference.Create(type, TypeContext.Output));
        return this;
    }

    public IInterfaceFieldDescriptor Field(string name)
    {
        var fieldDescriptor = Fields.FirstOrDefault(t => t.Definition.Name.EqualsOrdinal(name));

        if (fieldDescriptor is not null)
        {
            return fieldDescriptor;
        }

        fieldDescriptor = InterfaceFieldDescriptor.New(Context, name);
        Fields.Add(fieldDescriptor);
        return fieldDescriptor;
    }

    public IInterfaceTypeDescriptor ResolveAbstractType(
        ResolveAbstractType typeResolver)
    {
        Definition.ResolveAbstractType = typeResolver
            ?? throw new ArgumentNullException(nameof(typeResolver));
        return this;
    }

    public IInterfaceTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Definition.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IInterfaceTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Definition.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IInterfaceTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
        return this;
    }

    public static InterfaceTypeDescriptor New(IDescriptorContext context)
        => new(context);

    public static InterfaceTypeDescriptor New(IDescriptorContext context, Type clrType)
        => new(context, clrType);

    public static InterfaceTypeDescriptor<T> New<T>(IDescriptorContext context) => new(context);

    public static InterfaceTypeDescriptor FromSchemaType(
        IDescriptorContext context, Type schemaType)
    {
        var descriptor = New(context, schemaType);
        descriptor.Definition.RuntimeType = typeof(object);
        return descriptor;
    }

    public static InterfaceTypeDescriptor From(
        IDescriptorContext context,
        InterfaceTypeDefinition definition)
        => new(context, definition);

    public static InterfaceTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        InterfaceTypeDefinition definition)
        => new(context, definition);
}
