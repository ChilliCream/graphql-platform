using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Fusion.SourceSchema.Types;

public sealed class IsAttribute(string field) : ArgumentDescriptorAttribute
{
    public string Field { get; set; } = field;

    protected override void OnConfigure(
        IDescriptorContext context,
        IArgumentDescriptor descriptor,
        ParameterInfo parameter)
        => descriptor.Directive(new IsDirective(Field));
}
