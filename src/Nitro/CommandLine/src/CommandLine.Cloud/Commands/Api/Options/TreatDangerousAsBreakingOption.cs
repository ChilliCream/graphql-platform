using ChilliCream.Nitro.CommandLine.Cloud.Option;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Api.Options;

internal sealed class TreatDangerousAsBreakingOption : Option<bool?>
{
    public TreatDangerousAsBreakingOption() : base("--treat-dangerous-as-breaking")
    {
        Description = "Treat dangerous changes as breaking";
        IsRequired = false;
        this.DefaultFromEnvironmentValue("TREAT_DANGEROUS_AS_BREAKING");
    }
}
