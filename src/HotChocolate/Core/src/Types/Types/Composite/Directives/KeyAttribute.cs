using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Adds a @key directive to this object type to specify the fields that make up the unique key for an entity.
/// </para>
/// <para>
/// The @key directive is used to designate an entity's unique key,
/// which identifies how to uniquely reference an instance of
/// an entity across different source schemas.
/// </para>
/// <para>
/// One can specify multiple @key directives for an object type.
/// </para>
/// <para>
/// <code language="graphql">
/// # multiple single keys
/// type User @key(fields: "id") @key(fields: "email") {
///   id: ID!
///   name: String!
///   email: String!
/// }
///
/// # composite key
/// type Product @key(fields: "sku country") {
///   sku: String!
///   country: String!
/// }
/// </code>
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--key"/>
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Interface
    | AttributeTargets.Struct,
    AllowMultiple = true)]
public class KeyAttribute : DescriptorAttribute
{
    public KeyAttribute(string fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
    }

    /// <summary>
    /// Gets the fields that make up the unique key for an entity.
    /// </summary>
    [GraphQLType<NonNullType<FieldSelectionSetType>>]
    public string Fields { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor objectTypeDescriptor:
                objectTypeDescriptor.Key(Fields);
                break;

            case IInterfaceTypeDescriptor interfaceTypeDescriptor:
                interfaceTypeDescriptor.Key(Fields);
                break;

            default:
                throw new NotSupportedException(
                    $"The {descriptor.GetType().Name} descriptor is not supported.");
        }
    }
}
