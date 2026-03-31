namespace ChilliCream.Nitro.CommandLine.Options;

internal sealed class EnableGlobalObjectIdentificationOption : Option<bool?>
{
    public EnableGlobalObjectIdentificationOption()
        : base("--enable-global-object-identification")
    {
        Description = "Add the 'Query.node' field for global object identification";
    }
}
