using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Defines the properties of a GraphQL enum type.
/// </summary>
public class EnumTypeDefinition : TypeDefinitionBase<EnumTypeDefinitionNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
    /// </summary>
    public EnumTypeDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
    /// </summary>
    public EnumTypeDefinition(
        NameString name,
        string? description = null,
        Type? runtimeType = null)
        : base(runtimeType ?? typeof(object))
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Gets the enum values.
    /// </summary>
    public IBindableList<EnumValueDefinition> Values { get; } =
        new BindableList<EnumValueDefinition>();

    internal override IEnumerable<ITypeSystemMemberConfiguration> GetConfigurations()
    {
        List<ITypeSystemMemberConfiguration>? configs = null;

        if (HasConfigurations)
        {
            configs ??= new();
            configs.AddRange(Configurations);
        }

        foreach (EnumValueDefinition value in Values)
        {
            if (value.HasConfigurations)
            {
                configs ??= new();
                configs.AddRange(value.Configurations);
            }
        }

        return configs ?? Enumerable.Empty<ITypeSystemMemberConfiguration>();
    }
}
