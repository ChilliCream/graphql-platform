namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot.Extension.Options;

/// <summary>
/// Required, and the only accepted value today is <c>project</c>
/// (perles-net-k3j.16 non-goal: no user-scope extension install unless a
/// future spike live-verifies a user-scope extensions directory actually
/// loads anything - spike S5, perles-net-k3j.4 redo comment #94, found the
/// Copilot CLI's <c>EXTENSIONS</c> feature flag reports false on the machine
/// it ran on and could not confirm a live-loading user-scope extension).
/// Required rather than defaulted so installing the extension is always an
/// explicit, deliberate choice, unlike the hooks installers' scope options.
/// </summary>
internal sealed class CopilotExtensionScopeOption : Option<string>
{
    public const string Project = "project";

    public CopilotExtensionScopeOption() : base("--scope")
    {
        Description = "Where the extension is installed. Only 'project' "
            + "(<repo-root>/.github/extensions/nitro-mail/extension.mjs) is supported.";
        Required = true;
        AcceptOnlyFromAmong(Project);
    }
}
