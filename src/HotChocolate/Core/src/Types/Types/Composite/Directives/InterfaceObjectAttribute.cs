using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Marks the annotated class, struct or interface as an object type that acts as a stand-in for
/// an interface defined in another source schema, applying the @interfaceObject directive to it.
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--interfaceObject"/>
/// </para>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface)]
public sealed class InterfaceObjectAttribute
    : ObjectTypeDescriptorAttribute
    , ITypeAttribute
{
    public InterfaceObjectAttribute(string? name = null)
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

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type? type)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            descriptor.Name(Name);
        }

        descriptor.Extend().Configuration.Fields.BindingBehavior = BindingBehavior.Implicit;
        descriptor.InterfaceObject();
    }
}
