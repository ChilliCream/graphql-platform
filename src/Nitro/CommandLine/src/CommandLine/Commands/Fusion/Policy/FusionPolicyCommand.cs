namespace ChilliCream.Nitro.CommandLine.Commands.Fusion.Policy;

internal sealed class FusionPolicyCommand : Command
{
    public FusionPolicyCommand() : base("policy")
    {
        Description = "Manage Rego policy bundles.";

        Subcommands.Add(new FusionPolicyPackCommand());
    }
}
