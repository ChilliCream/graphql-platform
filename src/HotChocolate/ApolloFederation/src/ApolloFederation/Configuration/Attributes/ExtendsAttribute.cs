using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation;

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
public sealed class ExtendsAttribute : ObjectTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
        => descriptor.ExtendsType();
}
