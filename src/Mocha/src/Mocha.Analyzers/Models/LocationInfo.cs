namespace Mocha.Analyzers;

/// <summary>
/// An equatable representation of a source location that can safely
/// flow through the incremental pipeline without rooting Roslyn objects.
/// </summary>
/// <param name="FilePath">The file path of the source location.</param>
/// <param name="StartLine">The zero-based start line number.</param>
/// <param name="StartColumn">The zero-based start column number.</param>
/// <param name="EndLine">The zero-based end line number.</param>
/// <param name="EndColumn">The zero-based end column number.</param>
public sealed record LocationInfo(string FilePath, int StartLine, int StartColumn, int EndLine, int EndColumn);
