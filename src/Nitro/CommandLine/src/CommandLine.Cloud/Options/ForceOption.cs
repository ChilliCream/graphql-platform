namespace ChilliCream.Nitro.CLI.Option;

internal sealed class ForceOption : Option<bool>
{
    public ForceOption() : base("--force")
    {
        Description = "Will not ask for confirmation on deletes or overwrites.";
        IsRequired = false;
    }
}
