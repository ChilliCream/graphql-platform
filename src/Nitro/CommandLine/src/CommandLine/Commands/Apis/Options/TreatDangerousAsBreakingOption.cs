using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Apis.Options;

internal sealed class TreatDangerousAsBreakingOption : Option<bool?>
{
    public TreatDangerousAsBreakingOption() : base("--treat-dangerous-as-breaking")
    {
        Description = "Treat dangerous changes as breaking";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("TREAT_DANGEROUS_AS_BREAKING");
    }
}
