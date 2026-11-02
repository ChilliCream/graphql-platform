using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Claude;

/// <summary>
/// Tells the user which of Nitro's hook entries their harness's own switches keep from running
/// and how to switch them on. Nitro never changes those switches itself.
/// </summary>
internal static class HookSwitchGuidance
{
    public static void Write(
        INitroConsole console,
        string harnessName,
        string settingsPath,
        IReadOnlyList<string> disabledEvents,
        string? allHooksDisabledIn)
    {
        if (disabledEvents.Count > 0)
        {
            console.MarkupLine(
                ($"{harnessName} skips Nitro's {string.Join(", ", disabledEvents)} hooks because their entries in "
                    + $"'{settingsPath}' set \"enabled\": false. Remove that flag or set it to true to let them run.")
                .AsWarning());
        }

        if (allHooksDisabledIn is not null)
        {
            console.MarkupLine(
                ($"{harnessName} runs no hooks, Nitro's included, while '{allHooksDisabledIn}' sets "
                    + "\"disableAllHooks\": true. Set it to false or remove it to let them run.")
                .AsWarning());
        }
    }
}
