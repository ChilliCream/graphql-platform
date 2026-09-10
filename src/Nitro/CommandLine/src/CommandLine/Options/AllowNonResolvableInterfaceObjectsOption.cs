namespace ChilliCream.Nitro.CommandLine;

internal sealed class AllowNonResolvableInterfaceObjectsOption : Option<bool?>
{
    public const string OptionName = "--allow-non-resolvable-interface-objects";

    public AllowNonResolvableInterfaceObjectsOption()
        : base(OptionName)
    {
        Description = "Allow Apollo Federation interface objects without a resolvable key";
    }
}
