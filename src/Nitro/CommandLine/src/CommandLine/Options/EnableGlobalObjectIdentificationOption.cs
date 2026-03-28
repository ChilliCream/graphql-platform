using static ChilliCream.Nitro.CommandLine.CommandLineResources;

namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class EnableGlobalObjectIdentificationOption : Option<bool?>
{
    public EnableGlobalObjectIdentificationOption()
        : base("--enable-global-object-identification")
    {
        Description = ComposeCommand_EnableGlobalObjectIdentification_Description;
    }
}
