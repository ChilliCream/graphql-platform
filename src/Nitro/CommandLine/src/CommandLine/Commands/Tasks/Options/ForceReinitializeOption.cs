namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class ForceReinitializeOption : Option<bool>
{
    public ForceReinitializeOption() : base("--force")
    {
        Description = "Reinitialize an existing task workspace";
        Required = false;
    }
}
