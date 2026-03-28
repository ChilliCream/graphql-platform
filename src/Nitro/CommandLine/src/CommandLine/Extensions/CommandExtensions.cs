using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine;

internal static class CommandExtensions
{
    public static Command AddGlobalNitroOptions(this Command command)
    {
        command.Options.Add(Opt<CloudUrlOption>.Instance);
        command.Options.Add(Opt<ApiKeyOption>.Instance);
        command.Options.Add(Opt<OutputFormatOption>.Instance);

        return command;
    }
}
