using HotChocolate.Fusion;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class ShareableFieldRuntimeTypeRoutingOption
    : Option<ShareableFieldRuntimeTypeRouting?>
{
    public const string OptionName = "--shareable-field-runtime-type-routing";

    public ShareableFieldRuntimeTypeRoutingOption()
        : base(OptionName)
    {
        Description =
            "Choose how runtime types are routed for Apollo Federation shareable abstract fields";
        AcceptOnlyFromAmong("source-local", "common-runtime-types");
        CustomParser = result => result.Tokens.Single().Value switch
        {
            "source-local" => ShareableFieldRuntimeTypeRouting.SourceLocal,
            "common-runtime-types" => ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes,
            _ => null
        };
    }
}
