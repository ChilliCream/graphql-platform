using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;

internal static class TelemetryErrorRenderer
{
    public static int Render(INitroConsole console, string message, string hint)
    {
        console.Error.Write(new Text(message));
        console.Error.WriteLine();
        console.Error.Write(new Text($"hint: {hint}"));
        console.Error.WriteLine();

        return ExitCodes.Error;
    }
}
