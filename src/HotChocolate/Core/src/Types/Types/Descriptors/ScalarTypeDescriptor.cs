using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class ScalarTypeDescriptor
    : DescriptorBase<ScalarTypeDefinition>
    , IScalarTypeDescriptor
{
    protected ScalarTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
    }

    protected internal override ScalarTypeDefinition Definition { get; protected set; } = new();

    protected override void OnCreateDefinition(ScalarTypeDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (!Definition.AttributesAreApplied &&
            Definition.RuntimeType != typeof(object))
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.RuntimeType);
            Definition.AttributesAreApplied = true;
        }

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    public IScalarTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Definition.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IScalarTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Definition.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IScalarTypeDescriptor Directive(string name, params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
        return this;
    }

    public static ScalarTypeDescriptor New(
        IDescriptorContext context,
        string name,
        string? description,
        Type scalarType)
        => new(context) { Definition = { Name = name, Description = description, RuntimeType = scalarType } };
}
