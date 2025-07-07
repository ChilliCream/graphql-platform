using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[DirectiveType(
    DirectiveNames.Lookup.Name,
    DirectiveLocation.FieldDefinition,
    IsRepeatable = false)]
public sealed class Lookup
{
    private Lookup()
    {
    }

    /// <summary>
    /// The singleton instance of the <see cref="Lookup"/> directive.
    /// </summary>
    public static Lookup Instance { get; } = new();
}

[AttributeUsage(
    AttributeTargets.Property |
    AttributeTargets.Method,
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

public static class LookupDirectiveExtensions
{
    public static IObjectFieldDescriptor Lookup(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Types.Lookup.Instance);
    }

    public static IInterfaceFieldDescriptor Lookup(this IInterfaceFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Types.Lookup.Instance);
    }
}

/// <summary>
/// <para>
/// The @internal directive is used in combination with lookup fields and allows you
/// to declare internal types and fields. Internal types and fields do not appear in
/// the final client-facing composite schema and do not participate in the standard
/// schema-merging process. This allows a source schema to define lookup fields for
/// resolving entities that should not be accessible through the client-facing
/// composite schema.
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--internal"/>
/// </para>
/// <code>
/// type User @internal {
///   id: ID!
///   name: String!
/// }
///
/// directive @internal on OBJECT | FIELD_DEFINITION
/// </code>
/// </summary>
[DirectiveType(
    DirectiveNames.Internal.Name,
    DirectiveLocation.Object |
    DirectiveLocation.FieldDefinition,
    IsRepeatable = false)]
public sealed class Internal
{
    private Internal()
    {
    }

    /// <summary>
    /// The singleton instance of the <see cref="Internal"/> directive.
    /// </summary>
    public static Internal Instance { get; } = new();
}

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Property |
    AttributeTargets.Method,
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

public static class InternalDirectiveExtensions
{
    public static IObjectTypeDescriptor Internal(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Types.Internal.Instance);
    }

    public static IObjectFieldDescriptor Internal(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Types.Internal.Instance);
    }
}
