namespace Mocha.Analyzers;

/// <summary>
/// Carries the evaluated <c>MochaEmitSourceMetadata</c>, <c>ProjectDir</c>, <c>RepositoryUrl</c>,
/// and <c>SourceRevisionId</c> MSBuild properties to source generators.
/// </summary>
/// <param name="Emit">
/// A value indicating whether <c>SourceMetadata</c> assignments are emitted into generated
/// registration code.
/// </param>
/// <param name="ProjectDir">
/// The MSBuild project directory used to relativize declaration file paths, or <see langword="null"/>
/// when unavailable.
/// </param>
/// <param name="RepositoryUrl">
/// The source repository URL supplied by the build, or <see langword="null"/> when unavailable.
/// </param>
/// <param name="Commit">
/// The commit identifier supplied by the build, or <see langword="null"/> when unavailable.
/// </param>
public sealed record SourceMetadataOptionsInfo(
    bool Emit,
    string? ProjectDir,
    string? RepositoryUrl,
    string? Commit) : SyntaxInfo
{
    /// <summary>
    /// Gets the default options: metadata emission enabled and no project directory, repository URL,
    /// or commit available.
    /// </summary>
    public static SourceMetadataOptionsInfo Default { get; } = new(true, null, null, null);

    /// <inheritdoc />
    public override string OrderByKey => "SourceMetadataOptions";
}
