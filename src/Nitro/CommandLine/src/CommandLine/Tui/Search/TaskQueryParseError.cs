namespace ChilliCream.Nitro.CommandLine.Tui.Search;

/// <summary>
/// A parse failure on a search-mode query line: an unknown key: prefix, or
/// an invalid value for a recognized key. <see cref="Position"/> is the
/// zero-based index of the failing token in the input string.
/// </summary>
internal readonly record struct TaskQueryParseError(string Message, int Position);
