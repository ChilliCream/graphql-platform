namespace Mocha;

/// <summary>
/// Source declaration location captured by generated configuration metadata.
/// Line and column values are 1-based, matching editor coordinates, and the span covers
/// the entire declaration from its start to its end.
/// </summary>
/// <param name="File">
/// Name of the declaring source file.
/// </param>
/// <param name="Path">
/// Directory of the declaring source file relative to the repository root, empty when the file is
/// at the repository root, or null when the build does not provide source root information.
/// </param>
/// <param name="StartLine">The 1-based line number where the declaration starts.</param>
/// <param name="StartColumn">The 1-based column number where the declaration starts.</param>
/// <param name="EndLine">The 1-based line number where the declaration ends.</param>
/// <param name="EndColumn">The 1-based column number where the declaration ends.</param>
public sealed record DeclarationLocation(
    string File,
    string? Path,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
