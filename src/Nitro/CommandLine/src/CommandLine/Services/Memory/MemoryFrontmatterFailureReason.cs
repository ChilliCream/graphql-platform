namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Why <see cref="MemoryFrontmatterParser.TryParse"/> rejected a file.
/// </summary>
internal enum MemoryFrontmatterFailureReason
{
    /// <summary>
    /// The frontmatter block is missing its delimiters, a line is not a
    /// recognized <c>key: value</c> pair, or a key appears more than once.
    /// </summary>
    MalformedFrontmatter,

    /// <summary>A required key is missing from the frontmatter block.</summary>
    MissingRequiredKey,

    /// <summary>A key outside the restricted, known key set was present.</summary>
    UnknownKey,

    /// <summary>The <c>schema</c> value is not a version this build supports.</summary>
    UnsupportedSchemaVersion,

    /// <summary>A recognized key's value failed its own format rules.</summary>
    InvalidValue,

    /// <summary>The <c>id</c> value does not match the id the filename encodes.</summary>
    FilenameIdMismatch
}
