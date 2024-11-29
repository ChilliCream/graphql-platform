using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Types.FieldBindingFlags;

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

    /// <summary>
    /// Defines that static members are included.
    /// </summary>
    public bool IncludeStaticMembers { get; set; }

    TypeKind ITypeAttribute.Kind => TypeKind.Object;

    bool ITypeAttribute.IsTypeExtension => false;

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            descriptor.Name(Name);
        }

        var definition = descriptor.Extend().Definition;
        definition.Fields.BindingBehavior = BindingBehavior.Implicit;

        if (IncludeStaticMembers)
        {
            definition.FieldBindingFlags = Instance | Static;
        }
    }
}

/// <summary>
/// Specifies that the annotated class shall be
/// interpreted as a GraphQL object type.
/// This class is used by the Hot Chocolate source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ObjectTypeAttribute<T> : Attribute
{
    public Type Type => typeof(T);
}
