namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Ping.Options;

internal sealed class PingWorkerPidOption : Option<int>
{
    public PingWorkerPidOption() : base("--pid")
    {
        Description = "Internal: the target session process id. Set by the notifier, not for direct use.";
        Required = true;
        Validators.Add(result =>
        {
            if (result.GetValue(this) <= 0)
            {
                result.AddError("Option '--pid' must be a positive number.");
            }
        });
    }
}
