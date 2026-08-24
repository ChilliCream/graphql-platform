namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class TaskActorOption : Option<string>
{
    public TaskActorOption() : base("--actor")
    {
        Description = "The acting identity recorded on the audit log "
            + "(defaults to NITRO_TASK_ACTOR or the OS user name)";
        Required = false;
    }
}
