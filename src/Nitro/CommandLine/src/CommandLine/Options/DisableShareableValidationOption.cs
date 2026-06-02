namespace ChilliCream.Nitro.CommandLine;

internal sealed class DisableShareableValidationOption : Option<bool?>
{
    public DisableShareableValidationOption()
        : base("--disable-shareable-validation")
    {
        // TODO: Update this
        Description = "Disable shareable validation by automatically marking fields resolvable by multiple source schemas as shareable";
    }
}
