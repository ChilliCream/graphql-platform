using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Interface)]
public sealed class InterfaceTypeAttribute(string? name = null)
    : InterfaceTypeDescriptorAttribute
    , ITypeAttribute
{
    /// <summary>
    /// Gets or sets the GraphQL type name.
    /// </summary>
    public string? Name { get; set; } = name;

    /// <summary>
    /// Defines if this attribute is inherited. The default is <c>false</c>.
    /// </summary>
    public bool Inherited { get; set; }

    TypeKind ITypeAttribute.Kind => TypeKind.Interface;

    bool ITypeAttribute.IsTypeExtension => false;

    protected override void OnConfigure(
        IDescriptorContext context,
        IInterfaceTypeDescriptor descriptor,
        Type type)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            descriptor.Name(Name);
        }

        descriptor.Extend().Definition.Fields.BindingBehavior = BindingBehavior.Implicit;
    }
}

/// <summary>
/// Specifies that the annotated class shall be
/// interpreted as a GraphQL interface type.
/// This class is used by the Hot Chocolate source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class InterfaceTypeAttribute<T> : Attribute
{
    public Type Type => typeof(T);
}
