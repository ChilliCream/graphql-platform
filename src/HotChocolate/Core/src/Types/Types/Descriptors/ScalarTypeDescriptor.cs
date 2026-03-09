using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Descriptors;

public class ScalarTypeDescriptor
    : DescriptorBase<ScalarTypeConfiguration>
    , IScalarTypeDescriptor
{
    protected ScalarTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
    }

    protected internal override ScalarTypeConfiguration Configuration { get; protected set; } = new();

    protected override void OnCreateConfiguration(ScalarTypeConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (!Configuration.ConfigurationsAreApplied)
        {
            DescriptorAttributeHelper.ApplyConfiguration(
                Context,
                this,
                Configuration.RuntimeType);

            Configuration.ConfigurationsAreApplied = true;
        }

        base.OnCreateConfiguration(definition);

        Context.Descriptors.Pop();
    }

    public IScalarTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Configuration.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IScalarTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Configuration.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IScalarTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
        return this;
    }

    public static ScalarTypeDescriptor New(
        IDescriptorContext context,
        string name,
        string? description,
        Type scalarType)
        => new(context) { Configuration = { Name = name, Description = description, RuntimeType = scalarType } };
}
