#nullable enable
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

internal sealed class TagDirectiveConfigAttribute : DirectiveTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IDirectiveTypeDescriptor descriptor,
        Type type)
    {
        if (context.ContextData.TryGetValue(WellKnownContextData.TagOptions, out var value) &&
            value is TagOptions { Mode: TagMode.ApolloFederation, })
        {
            descriptor.Extend().Definition.Locations =
                DirectiveLocation.Object |
                DirectiveLocation.Interface |
                DirectiveLocation.Union |
                DirectiveLocation.InputObject |
                DirectiveLocation.Enum |
                DirectiveLocation.Scalar |
                DirectiveLocation.FieldDefinition |
                DirectiveLocation.InputFieldDefinition |
                DirectiveLocation.ArgumentDefinition |
                DirectiveLocation.EnumValue;
        }
    }
}
