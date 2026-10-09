namespace HotChocolate.Language;

/// <summary>
/// Represents ordinal-indexed variable name substitutions applied when formatting a packed UTF-8
/// syntax node. Each entry replaces the name of the variable with the matching document-scoped
/// ordinal. Entries are the replacement names without the leading dollar sign; the dollar sign
/// stays part of the verbatim source. The default value applies no substitutions.
/// </summary>
public readonly struct Utf8VariableNameMap
{
    private readonly ReadOnlyMemory<byte>[]? _names;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8VariableNameMap"/> struct over the
    /// specified replacement names. The array is held, not copied; the caller must not mutate it
    /// while formatting. The array is indexed by the document-scoped variable ordinal, and each
    /// entry is a replacement name without the leading dollar sign. An out-of-range or empty
    /// entry keeps the original variable name.
    /// </summary>
    /// <param name="names">
    /// The replacement names indexed by variable ordinal, without the leading dollar sign.
    /// </param>
    public Utf8VariableNameMap(ReadOnlyMemory<byte>[] names)
    {
        _names = names;
    }

    /// <summary>
    /// Gets a value indicating whether this map contains no substitutions.
    /// </summary>
    public bool IsEmpty => _names is null || _names.Length == 0;

    /// <summary>
    /// Tries to get the replacement name for the variable identified by
    /// <paramref name="ordinal"/>.
    /// </summary>
    /// <param name="ordinal">
    /// The document-scoped variable ordinal.
    /// </param>
    /// <param name="name">
    /// When this method returns <see langword="true"/>, the replacement name without the leading
    /// dollar sign; otherwise, an empty value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a replacement name exists; otherwise, <see langword="false"/>
    /// to keep the original name.
    /// </returns>
    internal bool TryGetReplacement(int ordinal, out ReadOnlyMemory<byte> name)
    {
        var names = _names;
        if (names is not null && (uint)ordinal < (uint)names.Length)
        {
            var candidate = names[ordinal];
            if (!candidate.IsEmpty)
            {
                name = candidate;
                return true;
            }
        }

        name = default;
        return false;
    }
}
