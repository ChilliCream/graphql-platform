namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class ForceReinitializeMailOption : Option<bool>
{
    public ForceReinitializeMailOption() : base("--force")
    {
        Description = "Reinitialize an existing mail workspace";
        Required = false;
    }
}
