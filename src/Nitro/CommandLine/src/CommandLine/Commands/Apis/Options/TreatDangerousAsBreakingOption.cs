using ChilliCream.Nitro.CommandLine;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class TreatDangerousAsBreakingOption : Option<bool?>
{
    public TreatDangerousAsBreakingOption() : base("--treat-dangerous-as-breaking")
    {
        Description = "Treat dangerous changes as breaking";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.TreatDangerousAsBreaking);
    }
}
