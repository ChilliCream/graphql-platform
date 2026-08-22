namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Session.Options;

internal sealed class ForceRebindSessionOption : Option<bool>
{
    public ForceRebindSessionOption() : base("--force-rebind")
    {
        Description = "Reclaim a session already explicitly claimed by a different actor, "
            + "resetting its delivery ledger and block budget";
        Required = false;
    }
}
