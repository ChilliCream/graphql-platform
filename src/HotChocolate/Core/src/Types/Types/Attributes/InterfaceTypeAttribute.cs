using System;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Interface)]
public sealed class InterfaceTypeAttribute
    : InterfaceTypeDescriptorAttribute
    , ITypeAttribute
{
    public InterfaceTypeAttribute(string? name = null)
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

    TypeKind ITypeAttribute.Kind => TypeKind.Interface;

    bool ITypeAttribute.IsTypeExtension => false;

    public override void OnConfigure(
        IDescriptorContext context,
        IInterfaceTypeDescriptor descriptor,
        Type type)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            descriptor.Name(Name);
        }
    }
}
