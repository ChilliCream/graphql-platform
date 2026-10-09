namespace Mocha;

/// <summary>
/// Metadata captured from a source declaration.
/// </summary>
public sealed record SourceMetadata
{
    /// <summary>
    /// The simple name of the assembly that declares the type.
    /// </summary>
    public string? Assembly { get; init; }

    /// <summary>
    /// The source repository URL, or null when the build does not provide it.
    /// </summary>
    public string? RepositoryUrl { get; init; }

    /// <summary>
    /// The commit identifier the source was built from, or null when the build does not provide it.
    /// </summary>
    public string? Commit { get; init; }

    /// <summary>
    /// XML documentation captured from the source declaration.
    /// </summary>
    public string? XmlDocumentation { get; init; }

    /// <summary>
    /// Location of the source declaration, using 1-based line and column coordinates.
    /// See <see cref="Mocha.DeclarationLocation"/> for the full contract.
    /// </summary>
    public DeclarationLocation? DeclarationLocation { get; init; }
}
