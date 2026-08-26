namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

internal sealed class MemoryCommand : Command
{
    public MemoryCommand() : base("memory")
    {
        Description = "Save and recall durable agent memory.";

        Subcommands.Add(new SaveMemoryCommand());
        Subcommands.Add(new LogMemoryCommand());
        Subcommands.Add(new UpdateMemoryCommand());
        Subcommands.Add(new ForgetMemoryCommand());
        Subcommands.Add(new ShowMemoryCommand());
        Subcommands.Add(new RecentMemoryCommand());
        Subcommands.Add(new SearchMemoryCommand());
        Subcommands.Add(new ContextMemoryCommand());
        Subcommands.Add(new PromoteMemoryCommand());
        Subcommands.Add(new TagsMemoryCommand());
        Subcommands.Add(new ReindexMemoryCommand());
        Subcommands.Add(new DoctorMemoryCommand());
        Subcommands.Add(new WhereMemoryCommand());
    }
}
