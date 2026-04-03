using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalFusionArchiveVersionOption : Option<Version>
{
    public OptionalFusionArchiveVersionOption() : base("--version")
    {
        Description = "The version of the archive to download";
        Required = false;
        DefaultValueFactory = _ => WellKnownVersions.LatestGatewayFormatVersion;
    }
}
