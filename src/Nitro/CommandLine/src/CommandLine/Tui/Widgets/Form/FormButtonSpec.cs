namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// One button in a <see cref="FormButtons"/> row.
/// </summary>
internal sealed record FormButtonSpec(string Id, string Label, ButtonKind Kind);
