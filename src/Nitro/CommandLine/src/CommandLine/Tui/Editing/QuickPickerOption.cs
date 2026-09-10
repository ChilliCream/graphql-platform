namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// One selectable choice in a <see cref="QuickPicker"/>: an id and the
/// pre-styled Spectre markup fragment rendered for it.
/// </summary>
internal sealed record QuickPickerOption(string Id, string Markup);
