namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Copilot.Extension.Options;

internal sealed class ForceCopilotExtensionOption : Option<bool>
{
    public ForceCopilotExtensionOption() : base("--force")
    {
        Description = "Overwrite an on-disk extension.mjs even if its content matches no known asset version.";
        Required = false;
    }
}
