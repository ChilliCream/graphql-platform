namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// One rendered line of a task detail body. <see cref="IsMarkup"/>
/// distinguishes section headers, section box borders, dependency rows, and
/// blocks rows, which already carry Spectre markup, from plain-text section
/// lines, including a section box's content rows, that still need
/// <c>Markup.Escape</c> before display. <see cref="IsSelectedRow"/> marks
/// the single dependency or blocks row the body's scroll position keeps
/// visible.
/// </summary>
internal readonly record struct TaskDetailBodyLine(string Content, bool IsMarkup, bool IsSelectedRow = false);
