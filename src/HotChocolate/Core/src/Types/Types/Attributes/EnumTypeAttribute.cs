using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Enum |
    AttributeTargets.Class |
    AttributeTargets.Struct)]
public sealed class EnumTypeAttribute
    : EnumTypeDescriptorAttribute
    , ITypeAttribute
{
    public EnumTypeAttribute(string? name = null)
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

    TypeKind ITypeAttribute.Kind => TypeKind.Enum;

    bool ITypeAttribute.IsTypeExtension => false;

    protected override void OnConfigure(
        IDescriptorContext context,
        IEnumTypeDescriptor descriptor,
        Type type)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            descriptor.Name(Name);
        }

        descriptor.BindValuesImplicitly();
    }
}
