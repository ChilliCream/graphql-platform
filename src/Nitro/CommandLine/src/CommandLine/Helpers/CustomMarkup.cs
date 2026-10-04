using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal readonly struct CustomMarkup
{
    public Renderable Content { get; init; }

    public Renderable SelectedContent { get; init; }

    public bool IsSelectable { get; init; }

    public bool IsHidden { get; init; }

    public Func<ConsoleKeyInfo, Task<InputAction?>>? HandleInput { get; init; }

    public static readonly CustomMarkup Hidden = new()
    {
        Content = Text.Empty,
        SelectedContent = Text.Empty,
        IsSelectable = false,
        IsHidden = true,
        HandleInput = null
    };
}
