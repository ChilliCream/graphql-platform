namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

internal sealed class MemoryCommand : Command
{
    public MemoryCommand() : base("memory")
    {
        Description = "Save and recall durable agent memory.";

        Subcommands.Add(new SaveMemoryCommand());
        Subcommands.Add(new UpdateMemoryCommand());
        Subcommands.Add(new ForgetMemoryCommand());
        Subcommands.Add(new ShowMemoryCommand());
        Subcommands.Add(new RecentMemoryCommand());
    }
}
