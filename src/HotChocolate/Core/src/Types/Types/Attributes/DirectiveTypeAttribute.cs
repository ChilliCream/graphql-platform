#nullable enable
using System;
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

    public DirectiveLocation Location { get; }

    public bool Inherited { get; set; }

    TypeKind ITypeAttribute.Kind => TypeKind.Directive;

    bool ITypeAttribute.IsTypeExtension => false;

    public override void OnConfigure(
        IDescriptorContext context,
        IDirectiveTypeDescriptor descriptor,
        Type type)
    {
        if (!string.IsNullOrEmpty(Name))
        {
            descriptor.Name(Name);
        }

        descriptor.Location(Location);
    }
}
