using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryReadScopeOption : Option<string>
{
    public MemoryReadScopeOption() : base("--scope")
    {
        Description = "The memory scope to read from (project, global, or all)";
        Required = false;
        DefaultValueFactory = _ => MemoryScopes.All;
        AcceptOnlyFromAmong(MemoryScopes.Project, MemoryScopes.Global, MemoryScopes.All);
    }
}
