namespace Mocha.Analyzers;

/// <summary>
/// Carries the evaluated <c>MochaEmitSourceMetadata</c>, <c>RepositoryUrl</c>,
/// <c>SourceRevisionId</c>, and <c>_MochaSourceRoots</c> MSBuild properties to source generators.
/// </summary>
/// <param name="Emit">
/// A value indicating whether <c>SourceMetadata</c> assignments are emitted into generated
/// registration code.
/// </param>
/// <param name="RepositoryUrl">
/// The source repository URL supplied by the build, or <see langword="null"/> when unavailable.
/// </param>
/// <param name="Commit">
/// The commit identifier supplied by the build, or <see langword="null"/> when unavailable.
/// </param>
/// <param name="SourceRoots">
/// The flattened SourceLink source roots supplied by the build as a single string, or
/// <see langword="null"/> when unavailable. Each record is
/// <c>Identity&gt;MappedPath&gt;SourceControl</c> and records are separated by <c>|</c>.
/// </param>
public sealed record SourceMetadataOptionsInfo(
    bool Emit,
    string? RepositoryUrl,
    string? Commit,
    string? SourceRoots) : SyntaxInfo
{
    /// <summary>
    /// Gets the default options: metadata emission enabled and no repository URL, commit, or source
    /// roots available.
    /// </summary>
    public static SourceMetadataOptionsInfo Default { get; } = new(true, null, null, null);

    /// <inheritdoc />
    public override string OrderByKey => "SourceMetadataOptions";
}
