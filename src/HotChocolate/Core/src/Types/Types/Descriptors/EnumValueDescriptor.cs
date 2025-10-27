#nullable disable

using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

public class EnumValueDescriptor
    : DescriptorBase<EnumValueConfiguration>
    , IEnumValueDescriptor
{
    protected EnumValueDescriptor(IDescriptorContext context, object runtimeValue)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(runtimeValue);

        Configuration.RuntimeValue = runtimeValue;
        Configuration.Description = context.Naming.GetEnumValueDescription(runtimeValue);
        Configuration.Member = context.TypeInspector.GetEnumValueMember(runtimeValue);

        if (context.Naming.IsDeprecated(runtimeValue, out var reason))
        {
            Deprecated(reason);
        }
    }

    protected EnumValueDescriptor(IDescriptorContext context, EnumValueConfiguration configuration)
        : base(context)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected internal override EnumValueConfiguration Configuration { get; protected set; } = new();

    protected override void OnCreateConfiguration(EnumValueConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (!Configuration.ConfigurationsAreApplied)
        {
            DescriptorAttributeHelper.ApplyConfiguration(
                Context,
                this,
                Configuration.Member,
                Configuration.Member,
                Configuration.Configurations);
            Configuration.ConfigurationsAreApplied = true;

            if (Configuration.Member is { } member
                && Context.TypeInspector.IsMemberIgnored(member))
            {
                Ignore();
            }
        }

        if (string.IsNullOrEmpty(definition.Name))
        {
            Configuration.Name = Context.Naming.GetEnumValueName(Configuration.RuntimeValue!);
        }

        base.OnCreateConfiguration(definition);

        Context.Descriptors.Pop();
    }

    public IEnumValueDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public IEnumValueDescriptor Description(string value)
    {
        Configuration.Description = value;
        return this;
    }

    public IEnumValueDescriptor Deprecated(string reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            return Deprecated();
        }

        Configuration.DeprecationReason = reason;
        return this;
    }

    public IEnumValueDescriptor Deprecated()
    {
        Configuration.DeprecationReason = DirectiveNames.Deprecated.Arguments.DefaultReason;
        return this;
    }

    public IEnumValueDescriptor Ignore(bool ignore = true)
    {
        Configuration.Ignore = ignore;
        return this;
    }

    public IEnumValueDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Configuration.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IEnumValueDescriptor Directive<T>()
        where T : class, new()
    {
        Configuration.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IEnumValueDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
        return this;
    }

    public static EnumValueDescriptor New(
        IDescriptorContext context,
        object value) =>
        new EnumValueDescriptor(context, value);

    public static EnumValueDescriptor From(
        IDescriptorContext context,
        EnumValueConfiguration definition) =>
        new EnumValueDescriptor(context, definition);
}
