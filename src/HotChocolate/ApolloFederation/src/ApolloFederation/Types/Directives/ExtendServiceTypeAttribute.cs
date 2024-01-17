using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @extends on OBJECT | INTERFACE
/// </code>
///
/// The @extends directive is used to represent type extensions in the schema. Federated extended types should have
/// corresponding @key directive defined that specifies primary key required to fetch the underlying object.
/// <example>
/// # extended from the Users service
/// type Foo @extends @key(fields: "id") {
///   id: ID
///   description: String
/// }
/// </example>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface)]
public sealed class ExtendServiceTypeAttribute : ObjectTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
        => descriptor.ExtendServiceType();
}
