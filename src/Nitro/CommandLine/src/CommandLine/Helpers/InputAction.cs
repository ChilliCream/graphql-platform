namespace ChilliCream.Nitro.CommandLine.Helpers;

internal record InputAction
{
    public record Select(int Index) : InputAction;

    public record Next(int Index) : InputAction;

    public record Break : InputAction;

    public record None : InputAction;
}
