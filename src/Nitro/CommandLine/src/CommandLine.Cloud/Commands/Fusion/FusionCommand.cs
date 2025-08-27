using ChilliCream.Nitro.CommandLine.Cloud.Option;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Fusion;

internal sealed class FusionCommand : Command
{
    public FusionCommand() : base("fusion")
    {
        AddGlobalOption(Opt<CloudUrlOption>.Instance);
        AddGlobalOption(Opt<ApiKeyOption>.Instance);

        AddCommand(new FusionPublishCommand());
    }
}
