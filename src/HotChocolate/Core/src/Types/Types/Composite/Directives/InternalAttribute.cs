#nullable enable

using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Applies the @internal directive to the type system member to declare it as an internal member.
/// Internal types and fields do not appear in the final client-facing composite schema and
/// do not participate in the standard schema-merging process. This allows a source schema to
/// define lookup fields for resolving entities that should not be accessible through the
/// client-facing composite schema.
/// </para>
/// <para>
/// <para>
/// <code language="graphql">
/// type User {
///   id: ID! @internal
///   name: String!
/// }
/// </code>
/// </para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--internal"/>
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Property
    | AttributeTargets.Method,
    AllowMultiple = false)]
public sealed class InternalAttribute : DescriptorAttribute
{
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor desc:
                desc.Internal();
                break;

            case IObjectFieldDescriptor desc:
                desc.Internal();
                break;

            default:
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("Internal directive is only supported on object types and field definitions.")
                        .SetExtension("member", element)
                        .SetExtension("descriptor", descriptor)
                        .Build());
        }
    }
}
