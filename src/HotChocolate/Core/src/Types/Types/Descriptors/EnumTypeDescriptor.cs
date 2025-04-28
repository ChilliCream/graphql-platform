using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

public class EnumTypeDescriptor
    : DescriptorBase<EnumTypeConfiguration>
    , IEnumTypeDescriptor
{
    protected EnumTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
        Configuration.RuntimeType = typeof(object);
        Configuration.Values.BindingBehavior = context.Options.DefaultBindingBehavior;
    }

    protected EnumTypeDescriptor(IDescriptorContext context, Type clrType)
        : base(context)
    {
        Configuration.RuntimeType = clrType ?? throw new ArgumentNullException(nameof(clrType));
        Configuration.Name = context.Naming.GetTypeName(clrType, TypeKind.Enum);
        Configuration.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Enum);
        Configuration.Values.BindingBehavior = context.Options.DefaultBindingBehavior;
    }

    protected EnumTypeDescriptor(IDescriptorContext context, EnumTypeConfiguration definition)
        : base(context)
    {
        Configuration = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal override EnumTypeConfiguration Configuration { get; protected set; } = new();

    protected ICollection<EnumValueDescriptor> Values { get; } =
        new List<EnumValueDescriptor>();

    protected override void OnCreateConfiguration(
        EnumTypeConfiguration definition)
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

        var values = Values.Select(t => t.CreateConfiguration()).ToDictionary(t => t.RuntimeValue);
        AddImplicitValues(definition, values);

        definition.Values.Clear();

        foreach (var value in values.Values)
        {
            definition.Values.Add(value);
        }

        base.OnCreateConfiguration(definition);

        Context.Descriptors.Pop();
    }

    protected void AddImplicitValues(
        EnumTypeConfiguration typeDefinition,
        IDictionary<object, EnumValueConfiguration> values)
    {
        if (typeDefinition.Values.IsImplicitBinding())
        {
            foreach (var value in Context.TypeInspector.GetEnumValues(typeDefinition.RuntimeType))
            {
                if (values.ContainsKey(value))
                {
                    continue;
                }

                var valueDefinition =
                    EnumValueDescriptor.New(Context, value)
                        .CreateConfiguration();

                if (valueDefinition.RuntimeValue is not null)
                {
                    values.Add(valueDefinition.RuntimeValue, valueDefinition);
                }
            }
        }
    }

    public IEnumTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public IEnumTypeDescriptor Description(string value)
    {
        Configuration.Description = value;
        return this;
    }

    public IEnumTypeDescriptor BindValues(
        BindingBehavior behavior)
    {
        Configuration.Values.BindingBehavior = behavior;
        return this;
    }

    public IEnumTypeDescriptor BindValuesExplicitly() =>
        BindValues(BindingBehavior.Explicit);

    public IEnumTypeDescriptor BindValuesImplicitly() =>
        BindValues(BindingBehavior.Implicit);

    public IEnumTypeDescriptor NameComparer(IEqualityComparer<string> comparer)
    {
        Configuration.NameComparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    public IEnumTypeDescriptor ValueComparer(IEqualityComparer<object> comparer)
    {
        Configuration.ValueComparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        return this;
    }

    public IEnumValueDescriptor Value<T>(T value)
    {
        var descriptor = Values.FirstOrDefault(t =>
            t.Configuration.RuntimeValue is not null &&
            t.Configuration.RuntimeValue.Equals(value));

        if (descriptor is not null)
        {
            return descriptor;
        }

        descriptor = EnumValueDescriptor.New(Context, value);
        Values.Add(descriptor);
        return descriptor;
    }

    public IEnumTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Configuration.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IEnumTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Configuration.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IEnumTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
        return this;
    }

    public static EnumTypeDescriptor New(
        IDescriptorContext context) =>
        new(context);

    public static EnumTypeDescriptor New(
        IDescriptorContext context,
        Type clrType) =>
        new(context, clrType);

    public static EnumTypeDescriptor<T> New<T>(
        IDescriptorContext context) =>
        new(context);

    public static EnumTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType)
    {
        var descriptor = New(context, schemaType);
        descriptor.Configuration.RuntimeType = typeof(object);
        return descriptor;
    }

    public static EnumTypeDescriptor From(
        IDescriptorContext context,
        EnumTypeConfiguration definition) =>
        new(context, definition);

    public static EnumTypeDescriptor From<T>(
        IDescriptorContext context,
        EnumTypeConfiguration definition) =>
        new(context, definition);
}
