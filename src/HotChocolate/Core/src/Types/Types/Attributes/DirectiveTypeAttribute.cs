#nullable enable
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class DirectiveTypeAttribute
    : DirectiveTypeDescriptorAttribute
    , ITypeAttribute
{
    public DirectiveTypeAttribute(DirectiveLocation location)
    {
        Location = location;
    }

    public DirectiveTypeAttribute(string name, DirectiveLocation location)
    {
        Name = name;
        Location = location;
    }

    /// <summary>
    /// Gets or sets the GraphQL directive.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the GraphQL directive location.
    /// </summary>
    public DirectiveLocation Location { get; }

    /// <summary>
    /// Defines if the directive can be declared multiple times at a single location.
    /// </summary>
    public bool IsRepeatable { get; set; }

    public bool Inherited { get; set; }

    TypeKind ITypeAttribute.Kind => TypeKind.Directive;

    bool ITypeAttribute.IsTypeExtension => false;

    protected override void OnConfigure(
        IDescriptorContext context,
        IDirectiveTypeDescriptor descriptor,
        Type type)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            descriptor.Name(Name);
        }

        descriptor.Location(Location);

        if (IsRepeatable)
        {
            descriptor.Repeatable();
        }

        descriptor.Extend().Definition.Arguments.BindingBehavior = BindingBehavior.Implicit;
    }
}
