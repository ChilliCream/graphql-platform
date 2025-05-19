using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors;

public class InterfaceTypeDescriptor
    : DescriptorBase<InterfaceTypeConfiguration>
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

        Configuration.RuntimeType = clrType;
        Configuration.Name = context.Naming.GetTypeName(clrType, TypeKind.Interface);
        Configuration.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Interface);
    }

    protected InterfaceTypeDescriptor(
        IDescriptorContext context)
        : base(context)
    {
        Configuration.RuntimeType = typeof(object);
    }

    protected InterfaceTypeDescriptor(
        IDescriptorContext context,
        InterfaceTypeConfiguration definition)
        : base(context)
    {
        Configuration = definition ?? throw new ArgumentNullException(nameof(definition));

        foreach (var field in definition.Fields)
        {
            Fields.Add(InterfaceFieldDescriptor.From(Context, field));
        }
    }

    protected internal override InterfaceTypeConfiguration Configuration { get; protected set; } =
        new();

    protected ICollection<InterfaceFieldDescriptor> Fields { get; } =
        new List<InterfaceFieldDescriptor>();

    protected override void OnCreateConfiguration(
        InterfaceTypeConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (!Configuration.AttributesAreApplied && Configuration.RuntimeType != typeof(object))
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Configuration.RuntimeType);
            Configuration.AttributesAreApplied = true;
        }

        var fields = new Dictionary<string, InterfaceFieldConfiguration>();
        var handledMembers = new HashSet<MemberInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Fields.Select(t => t.CreateConfiguration()),
            f => f.Member,
            fields,
            handledMembers);

        OnCompleteFields(fields, handledMembers);

        Configuration.Fields.Clear();
        Configuration.Fields.AddRange(fields.Values);

        base.OnCreateConfiguration(definition);

        Context.Descriptors.Pop();
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, InterfaceFieldConfiguration> fields,
        ISet<MemberInfo> handledMembers)
    {
    }

    public IInterfaceTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public IInterfaceTypeDescriptor Description(string value)
    {
        Configuration.Description = value;
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

        Configuration.Interfaces.Add(
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

        Configuration.Interfaces.Add(new SchemaTypeReference(type));
        return this;
    }

    public IInterfaceTypeDescriptor Implements(NamedTypeNode type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        Configuration.Interfaces.Add(TypeReference.Create(type, TypeContext.Output));
        return this;
    }

    public IInterfaceFieldDescriptor Field(string name)
    {
        var fieldDescriptor = Fields.FirstOrDefault(t => t.Configuration.Name.EqualsOrdinal(name));

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
        Configuration.ResolveAbstractType = typeResolver
            ?? throw new ArgumentNullException(nameof(typeResolver));
        return this;
    }

    public IInterfaceTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Configuration.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IInterfaceTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Configuration.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IInterfaceTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
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
        descriptor.Configuration.RuntimeType = typeof(object);
        return descriptor;
    }

    public static InterfaceTypeDescriptor From(
        IDescriptorContext context,
        InterfaceTypeConfiguration definition)
        => new(context, definition);

    public static InterfaceTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        InterfaceTypeConfiguration definition)
        => new(context, definition);
}
