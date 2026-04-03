namespace ChilliCream.Nitro.CommandLine;

internal sealed class EnableGlobalObjectIdentificationOption : Option<bool?>
{
    public EnableGlobalObjectIdentificationOption()
        : base("--enable-global-object-identification")
    {
        Description = "Add the 'Query.node' field for global object identification";
    }
}
