#nullable enable

using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Makes the fields in scope or the field annotated with this attribute shareable by adding the @shareable directive.
/// </para>
/// <para>
/// By default, only a single source schema is allowed to contribute
/// a particular field to an object type.
/// </para>
/// <para>
/// This prevents source schemas from inadvertently defining similarly named
/// fields that are not semantically equivalent.
/// </para>
/// <para>
/// This is why fields must be explicitly marked as @shareable to allow multiple source
/// schemas to define them, ensuring that the decision to serve a field from
/// more than one source schema is intentional and coordinated.
/// </para>
/// <para>
/// <code language="csharp">
/// type User {
///   name: String! @shareable
///   email: String!
/// }
/// </code>
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--shareable"/>
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
public sealed class ShareableAttribute : DescriptorAttribute
{
    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IObjectTypeDescriptor desc:
                desc.Shareable();
                break;

            case IObjectFieldDescriptor desc:
                desc.Shareable();
                break;

            default:
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("Shareable directive is only supported on object types and object fields.")
                        .SetExtension("member", element)
                        .SetExtension("descriptor", descriptor)
                        .Build());
        }
    }
}
