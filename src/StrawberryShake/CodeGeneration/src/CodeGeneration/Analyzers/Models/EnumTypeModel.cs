using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

/// <summary>
/// Represents an enum type model.
/// </summary>
public sealed class EnumTypeModel : LeafTypeModel
{
    /// <summary>
    /// Initialize new instance of <see cref="EnumTypeModel"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the enum type.
    /// </param>
    /// <param name="description">
    /// The description of the enum type.
    /// </param>
    /// <param name="type">
    /// The enum type.
    /// </param>
    /// <param name="underlyingType">
    /// The underlying runtime type.
    /// </param>
    /// <param name="values">
    /// The enum values.
    /// </param>
    public EnumTypeModel(
        string name,
        string? description,
        IEnumType type,
        string? underlyingType,
        IReadOnlyList<EnumValueModel> values)
        : base(name, description, type, TypeNames.String, name)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        UnderlyingType = underlyingType;
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }

    /// <summary>
    /// Gets the enum type.
    /// </summary>
    public new IEnumType Type { get; }

    /// <summary>
    /// Gets the underlying type name.
    /// </summary>
    public string? UnderlyingType { get; }

    /// <summary>
    /// Gets the enum values models.
    /// </summary>
    public IReadOnlyList<EnumValueModel> Values { get; }
}
