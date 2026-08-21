namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryActorOption : Option<string>
{
    public MemoryActorOption() : base("--actor")
    {
        Description = "The acting identity recorded on memory writes "
            + "(defaults to NITRO_MEMORY_ACTOR, NITRO_TASK_ACTOR, or the OS user name)";
        Required = false;
    }
}
