using HotChocolate.Types;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

/// <summary>
/// Represents an enum value.
/// </summary>
public sealed class EnumValueModel
{
    public EnumValueModel(
        string name,
        string? description,
        IEnumValue value,
        string? underlyingValue)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        UnderlyingValue = underlyingValue;
    }

    /// <summary>
    /// The enum value name. This is the string that is being used to
    /// </summary>
    public string Name { get; }

    public string? Description { get; }

    public IEnumValue Value { get; }

    public string? UnderlyingValue { get; }
}
