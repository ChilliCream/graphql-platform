namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// A rectangular window over a cell buffer.
/// </summary>
internal readonly record struct CanvasViewport(int X, int Y, int Width, int Height);
