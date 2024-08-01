using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// The `@oneOf` directive is used within the type system definition language
/// to indicate an Input Object is a Oneof Input Object.
///
/// <code>
/// input UserUniqueCondition @oneOf {
///   id: ID
///   username: String
///   organizationAndEmail: OrganizationAndEmailInput
/// }
/// </code>
/// </summary>
public sealed class OneOfAttribute : InputObjectTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IInputObjectTypeDescriptor descriptor,
        Type type)
        => descriptor.OneOf();
}
