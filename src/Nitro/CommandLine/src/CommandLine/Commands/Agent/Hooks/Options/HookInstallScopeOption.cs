using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;

internal sealed class HookInstallScopeOption : Option<string>
{
    public HookInstallScopeOption() : base("--scope")
    {
        Description = "Where the Claude Code settings file lives: 'user' (~/.claude/settings.json) "
            + "or 'project' (<workspace>/.claude/settings.json)";
        Required = false;
        DefaultValueFactory = _ => HookInstallScopes.User;
        AcceptOnlyFromAmong(HookInstallScopes.User, HookInstallScopes.Project);
    }
}
