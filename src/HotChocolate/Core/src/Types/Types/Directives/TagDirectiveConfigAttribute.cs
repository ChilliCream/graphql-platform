#nullable enable
using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

internal sealed class TagDirectiveConfigAttribute : DirectiveTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context, 
        IDirectiveTypeDescriptor descriptor, 
        Type type)
    {
        descriptor.Description(
            """
            The @tag directive is used to apply arbitrary string
            metadata to a schema location. Custom tooling can use
            this metadata during any step of the schema delivery flow,
            including composition, static analysis, and documentation.

            interface Book {
              id: ID! @tag(name: "your-value")
              title: String!
              author: String!
            }
            """);
        
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