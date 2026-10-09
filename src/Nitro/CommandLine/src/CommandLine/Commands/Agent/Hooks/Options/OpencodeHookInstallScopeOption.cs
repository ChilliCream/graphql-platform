using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;

internal sealed class OpencodeHookInstallScopeOption : Option<string>
{
    public OpencodeHookInstallScopeOption() : base("--scope")
    {
        Description = "Where the Opencode plugin lives: 'user' ($XDG_CONFIG_HOME/opencode/plugins) or "
            + "'project' (<workspace>/.opencode/plugin)";
        Required = false;
        DefaultValueFactory = _ => HookInstallScopes.User;
        AcceptOnlyFromAmong(HookInstallScopes.User, HookInstallScopes.Project);
    }
}
