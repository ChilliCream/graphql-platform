using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Data.Sorting;

public class SortEnumTypeDescriptor
    : DescriptorBase<SortEnumTypeDefinition>,
      ISortEnumTypeDescriptor
{
    protected SortEnumTypeDescriptor(
        IDescriptorContext context,
        Type clrType,
        string? scope)
        : base(context)
    {
        Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.Enum);
        Definition.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Enum);
        Definition.EntityType = clrType;
        Definition.RuntimeType = typeof(object);
        Definition.Values.BindingBehavior = context.Options.DefaultBindingBehavior;
        Definition.Scope = scope;
    }

    protected SortEnumTypeDescriptor(
        IDescriptorContext context,
        SortEnumTypeDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal override SortEnumTypeDefinition Definition { get; protected set; } = new();

    protected ICollection<SortEnumValueDescriptor> Values { get; } =
        new List<SortEnumValueDescriptor>();

    protected override void OnCreateDefinition(
        SortEnumTypeDefinition definition)
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

        var values = Values.Select(t => t.CreateDefinition())
            .OfType<SortEnumValueDefinition>()
            .ToDictionary(t => t.Value);

        definition.Values.Clear();

        foreach (var value in values.Values)
        {
            definition.Values.Add(value);
        }

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    public ISortEnumTypeDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public ISortEnumTypeDescriptor Description(string value)
    {
        Definition.Description = value;
        return this;
    }

    public ISortEnumValueDescriptor Operation(int operation)
    {
        var descriptor = Values
            .FirstOrDefault(
                t =>
                    t.Definition.RuntimeValue is not null &&
                    t.Definition.RuntimeValue.Equals(operation));

        if (descriptor is not null)
        {
            return descriptor;
        }

        descriptor = SortEnumValueDescriptor.New(Context, Definition.Scope, operation);
        Values.Add(descriptor);
        return descriptor;
    }

    public ISortEnumTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Definition.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public ISortEnumTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Definition.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public ISortEnumTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
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
        SortEnumTypeDefinition definition)
        => new(context, definition);
}
