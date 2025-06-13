using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// Indicates that the given field, argument, input field, or enum value requires giving explicit
/// consent before being used.
///
/// <code>
/// type Session {
///     id: ID!
///     title: String!
///     # [...]
///     startInstant: Instant @requiresOptIn(feature: "experimentalInstantApi")
///     endInstant: Instant @requiresOptIn(feature: "experimentalInstantApi")
/// }
/// </code>
/// </summary>
public sealed class RequiresOptInDirectiveType : DirectiveType<RequiresOptInDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<RequiresOptInDirective> descriptor)
    {
        descriptor
            .Name(DirectiveNames.RequiresOptIn.Name)
            .Description(TypeResources.RequiresOptInDirectiveType_TypeDescription)
            .Location(DirectiveLocation.ArgumentDefinition)
            .Location(DirectiveLocation.EnumValue)
            .Location(DirectiveLocation.FieldDefinition)
            .Location(DirectiveLocation.InputFieldDefinition);

        descriptor
            .Argument(t => t.Feature)
            .Name(DirectiveNames.RequiresOptIn.Arguments.Feature)
            .Description(TypeResources.RequiresOptInDirectiveType_FeatureDescription)
            .Type<NonNullType<StringType>>();
    }
}
