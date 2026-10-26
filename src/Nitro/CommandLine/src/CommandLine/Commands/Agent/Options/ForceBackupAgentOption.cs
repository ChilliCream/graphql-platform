namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class ForceBackupAgentOption : Option<bool>
{
    public ForceBackupAgentOption() : base("--force")
    {
        Description = "Overwrite the archive if it already exists";
        Required = false;
    }
}
