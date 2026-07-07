namespace Mocha;

/// <summary>
/// Source declaration location captured by generated configuration metadata.
/// Line and column values are 1-based, matching editor coordinates, and the span covers
/// the entire declaration from its start to its end.
/// </summary>
/// <param name="File">
/// The source file path, relative to the project directory using forward slashes,
/// or the file name alone when a project-relative path is not available.
/// </param>
/// <param name="StartLine">The 1-based line number where the declaration starts.</param>
/// <param name="StartColumn">The 1-based column number where the declaration starts.</param>
/// <param name="EndLine">The 1-based line number where the declaration ends.</param>
/// <param name="EndColumn">The 1-based column number where the declaration ends.</param>
public sealed record DeclarationLocation(
    string File,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
