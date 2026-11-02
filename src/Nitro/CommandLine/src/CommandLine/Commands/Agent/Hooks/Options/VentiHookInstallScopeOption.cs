using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Options;

internal sealed class VentiHookInstallScopeOption : Option<string>
{
    public VentiHookInstallScopeOption() : base("--scope")
    {
        Description = "Where the Venti settings file lives: 'user' (~/.venti/settings.json) "
            + "or 'project' (./.venti/settings.json, read when Venti is launched from the current directory)";
        Required = false;
        DefaultValueFactory = _ => HookInstallScopes.User;
        AcceptOnlyFromAmong(HookInstallScopes.User, HookInstallScopes.Project);
    }
}
