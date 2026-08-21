using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory.Options;

internal sealed class MemoryWriteScopeOption : Option<string>
{
    public MemoryWriteScopeOption() : base("--scope")
    {
        Description = "The memory scope to write to (project or global)";
        Required = false;
        DefaultValueFactory = _ => MemoryScopes.Project;
        AcceptOnlyFromAmong(MemoryScopes.Project, MemoryScopes.Global);
    }
}
