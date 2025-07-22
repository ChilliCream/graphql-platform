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
        if (context.Features.TryGet<TagOptions>(out var options)
            && options.Mode == TagMode.ApolloFederation)
        {
            descriptor.Extend().Configuration.Locations =
                DirectiveLocation.Object
                | DirectiveLocation.Interface
                | DirectiveLocation.Union
                | DirectiveLocation.InputObject
                | DirectiveLocation.Enum
                | DirectiveLocation.Scalar
                | DirectiveLocation.FieldDefinition
                | DirectiveLocation.InputFieldDefinition
                | DirectiveLocation.ArgumentDefinition
                | DirectiveLocation.EnumValue;
        }
    }
}
