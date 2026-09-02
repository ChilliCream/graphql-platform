namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

/// <summary>
/// Installs, inspects, and removes Nitro's Opencode plugin shim. Teammate
/// context is best effort, so an unavailable Nitro service never blocks Opencode.
/// </summary>
internal sealed class OpencodeHooksCommand : Command
{
    public OpencodeHooksCommand() : base("opencode")
    {
        Description = "Install, inspect, and remove Nitro's fail-open Opencode teammate-context plugin. "
            + "Project plugins are normally committed; add .opencode/plugin/nitro-hooks.js to .gitignore "
            + "when the setup is local only.";

        Subcommands.Add(new InstallOpencodeHooksCommand());
        Subcommands.Add(new StatusOpencodeHooksCommand());
        Subcommands.Add(new UninstallOpencodeHooksCommand());
    }
}
