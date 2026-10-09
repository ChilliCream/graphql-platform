using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionInputFieldDefinitionCollection
    : FusionFieldDefinitionCollection<FusionInputFieldDefinition>
    , IReadOnlyFieldDefinitionCollection<IInputValueDefinition>
{
    private readonly byte[][] _utf8AccessibleNames;
    private readonly FusionInputFieldDefinition[] _utf8AccessibleFields;

    public FusionInputFieldDefinitionCollection(FusionInputFieldDefinition[] fields)
        : base(fields)
    {
        _utf8AccessibleNames = Utf8NameIndex.Create(fields, Count, static f => f.Name, out _utf8AccessibleFields);
    }

    /// <summary>
    /// Gets the accessible field whose name matches the specified UTF-8 encoded <paramref name="utf8Name"/>.
    /// </summary>
    /// <param name="utf8Name">
    /// The GraphQL field name as UTF-8 bytes.
    /// </param>
    /// <param name="field">
    /// When this method returns, contains the field with the specified name if found;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the field was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetField(
        ReadOnlySpan<byte> utf8Name,
        [NotNullWhen(true)] out FusionInputFieldDefinition? field)
    {
        var index = Utf8NameIndex.IndexOf(_utf8AccessibleNames, utf8Name);

        if (index == -1)
        {
            field = null;
            return false;
        }

        field = _utf8AccessibleFields[index];
        return true;
    }

    IInputValueDefinition IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.this[string name]
        => this[name];

    IInputValueDefinition IReadOnlyList<IInputValueDefinition>.this[int index]
        => this[index];

    bool IReadOnlyFieldDefinitionCollection<IInputValueDefinition>.TryGetField(
        string name,
        [NotNullWhen(true)] out IInputValueDefinition? field)
    {
        if (TryGetField(name, out var f))
        {
            field = f;
            return true;
        }

        field = null;
        return false;
    }

    IEnumerator<IInputValueDefinition> IEnumerable<IInputValueDefinition>.GetEnumerator()
        => GetEnumerator();

    public static FusionInputFieldDefinitionCollection Empty { get; } = new([]);
}
