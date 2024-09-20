using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Defines the properties of a GraphQL enum type.
/// </summary>
public class EnumTypeDefinition : TypeDefinitionBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
    /// </summary>
    public EnumTypeDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinition"/>.
    /// </summary>
    public EnumTypeDefinition(
        string name,
        string? description = null,
        Type? runtimeType = null)
        : base(runtimeType ?? typeof(object))
    {
        Name = name.EnsureGraphQLName();
        Description = description;
    }

    /// <summary>
    /// Gets or sets the enum name comparer that will be used to validate
    /// if an enum name represents an enum value of this type.
    /// </summary>
    public IEqualityComparer<string> NameComparer { get; set; } = StringComparer.Ordinal;

    /// <summary>
    /// Gets or sets the runtime value comparer that will be used to validate
    /// if a runtime value represents a GraphQL enum value of this type.
    /// </summary>
    public IEqualityComparer<object> ValueComparer { get; set; } = DefaultValueComparer.Instance;

    /// <summary>
    /// Gets the enum values.
    /// </summary>
    public IBindableList<EnumValueDefinition> Values { get; } =
        new BindableList<EnumValueDefinition>();

    public override IEnumerable<ITypeSystemMemberConfiguration> GetConfigurations()
    {
        List<ITypeSystemMemberConfiguration>? configs = null;

        if (HasConfigurations)
        {
            configs ??= [];
            configs.AddRange(Configurations);
        }

        foreach (var value in Values)
        {
            if (value.HasConfigurations)
            {
                configs ??= [];
                configs.AddRange(value.Configurations);
            }
        }

        return configs ?? Enumerable.Empty<ITypeSystemMemberConfiguration>();
    }

    private sealed class DefaultValueComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object? x, object? y)
            => Equals(x, y);

        int IEqualityComparer<object>.GetHashCode(object obj)
            => obj.GetHashCode();

        public static DefaultValueComparer Instance { get; } = new();
    }
}
