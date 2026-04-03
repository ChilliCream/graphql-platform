using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class OptionalFusionArchiveVersionOption : Option<Version>
{
    private const string OptionName = "--version";

    public OptionalFusionArchiveVersionOption() : base(OptionName)
    {
        Description = "The version of the archive to download";
        Required = false;
        DefaultValueFactory = _ => WellKnownVersions.LatestGatewayFormatVersion;
        CustomParser = result =>
        {
            var versionStr = result.Tokens.Single().Value;
            if (Version.TryParse(versionStr, out var version))
            {
                return version;
            }
            else
            {
                result.AddError($"Option '--{OptionName}' received an invalid value: {versionStr}");
                return null;
            }
        };
    }
}
