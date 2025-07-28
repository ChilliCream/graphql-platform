using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// The @lookup directive is used within a source schema to specify output fields
/// that can be used by the distributed GraphQL executor to resolve an entity by
/// a stable key.
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--lookup"/>
/// </para>
/// <code>
/// type Query {
///   productById(id: ID!): Product @lookup
/// }
///
/// directive @lookup on FIELD_DEFINITION
/// </code>
/// </summary>
[AttributeUsage(
    AttributeTargets.Property
    | AttributeTargets.Method,
    AllowMultiple = false)]
public sealed class LookupAttribute : DescriptorAttribute
{
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectFieldDescriptor desc:
                desc.Lookup();
                break;

            default:
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("Lookup directive is only supported on field definitions of objects types.")
                        .SetExtension("member", element)
                        .SetExtension("descriptor", descriptor)
                        .Build());
        }
    }
}
