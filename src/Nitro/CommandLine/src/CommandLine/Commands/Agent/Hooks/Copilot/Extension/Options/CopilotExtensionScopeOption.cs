namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot.Extension.Options;

/// <summary>
/// Required. The only accepted value is <c>project</c> because user-scope
/// extension installation is not supported.
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
