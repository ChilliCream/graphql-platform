using System;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Specifies that the annotated class, struct or interface shall be
/// interpreted as a GraphQL object type.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface)]
public sealed class ObjectTypeAttribute
    : ObjectTypeDescriptorAttribute
    , ITypeAttribute
{
    public ObjectTypeAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// Gets or sets the GraphQL type name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Defines if this attribute is inherited. The default is <c>false</c>.
    /// </summary>
    public bool Inherited { get; set; }

    TypeKind ITypeAttribute.Kind => TypeKind.Object;

    bool ITypeAttribute.IsTypeExtension => false;

    public override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            descriptor.Name(Name);
        }
    }
}
