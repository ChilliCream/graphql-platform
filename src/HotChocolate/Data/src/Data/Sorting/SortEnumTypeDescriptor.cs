using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Data.Sorting;

public class SortEnumTypeDescriptor
    : DescriptorBase<SortEnumTypeConfiguration>,
      ISortEnumTypeDescriptor
{
    protected SortEnumTypeDescriptor(
        IDescriptorContext context,
        Type clrType,
        string? scope)
        : base(context)
    {
        Configuration.Name = context.Naming.GetTypeName(clrType, TypeKind.Enum);
        Configuration.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Enum);
        Configuration.EntityType = clrType;
        Configuration.RuntimeType = typeof(object);
        Configuration.Values.BindingBehavior = context.Options.DefaultBindingBehavior;
        Configuration.Scope = scope;
    }

    protected SortEnumTypeDescriptor(
        IDescriptorContext context,
        SortEnumTypeConfiguration configuration)
        : base(context)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected internal override SortEnumTypeConfiguration Configuration { get; protected set; } = new();

    protected ICollection<SortEnumValueDescriptor> Values { get; } = [];

    protected override void OnCreateConfiguration(
        SortEnumTypeConfiguration configuration)
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

        var values = Values.Select(t => t.CreateConfiguration())
            .OfType<SortEnumValueConfiguration>()
            .ToDictionary(t => t.Value);

        configuration.Values.Clear();

        foreach (var value in values.Values)
        {
            configuration.Values.Add(value);
        }

        base.OnCreateConfiguration(configuration);

        Context.Descriptors.Pop();
    }

    public ISortEnumTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public ISortEnumTypeDescriptor Description(string value)
    {
        Configuration.Description = value;
        return this;
    }

    public ISortEnumValueDescriptor Operation(int operation)
    {
        var descriptor = Values
            .FirstOrDefault(t => t.Configuration.RuntimeValue?.Equals(operation) == true);

        if (descriptor is not null)
        {
            return descriptor;
        }

        descriptor = SortEnumValueDescriptor.New(Context, Configuration.Scope, operation);
        Values.Add(descriptor);
        return descriptor;
    }

    public ISortEnumTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Configuration.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public ISortEnumTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Configuration.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public ISortEnumTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
        return this;
    }

    public static SortEnumTypeDescriptor New(IDescriptorContext context, Type type, string? scope)
        => new(context, type, scope);

    public static SortEnumTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType,
        string? scope)
    {
        var descriptor = New(context, schemaType, scope);
        return descriptor;
    }

    public static SortEnumTypeDescriptor From(
        IDescriptorContext context,
        SortEnumTypeConfiguration configuration)
        => new(context, configuration);
}
