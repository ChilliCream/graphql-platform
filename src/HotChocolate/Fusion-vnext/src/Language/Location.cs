namespace HotChocolate.Fusion;

/// <summary>
/// The location of a syntax node.
/// </summary>
/// <param name="Start">The start position of the syntax node.</param>
/// <param name="End">The end position of the syntax node.</param>
/// <param name="Line">The line in which the syntax node is located.</param>
/// <param name="Column">The column in which the syntax node is located.</param>
internal sealed record Location(int Start, int End, int Line, int Column);
