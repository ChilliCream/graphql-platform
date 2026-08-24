using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Static, free doctor checks for a harness's installed hook entries: does
/// each managed event's live entry match what <c>install</c> would write
/// today (current vs. outdated, the same distinction <c>hooks status</c>
/// reports), and does the sidecar's provenance record agree with what is
/// actually on disk (an entry present with no sidecar record, a sidecar
/// record for an entry that is no longer there, or an entry whose text does
/// not match its sidecar record - all signs of a foreign edit or a lost
/// sidecar). Returns null when the harness has never been installed here
/// (no entry, no sidecar record for it): an opted-out harness is not a
/// doctor finding.
/// </summary>
internal static class DoctorHooksCheck
{
    public static async Task<DoctorAgentCommand.HookHarnessDoctorResult?> CheckClaudeAsync(
        IClaudeHooksInstallerService installer,
        IClaudeHooksSidecarStore sidecarStore,
        string scope,
        CancellationToken cancellationToken)
    {
        var status = await installer.StatusAsync(scope, cancellationToken);
        var sidecar = await sidecarStore.ReadAsync(cancellationToken);
        var entries = sidecar.EntriesFor(status.SettingsPath);

        return Evaluate(
            status.SettingsPath,
            status.Events,
            eventName => entries.TryGetValue(eventName, out var entry) ? entry.Command : null,
            "nitro agent hooks claude install",
            "nitro agent hooks claude uninstall");
    }

    public static async Task<DoctorAgentCommand.HookHarnessDoctorResult?> CheckCopilotAsync(
        ICopilotHooksInstallerService installer,
        ICopilotHooksSidecarStore sidecarStore,
        CancellationToken cancellationToken)
    {
        var status = await installer.StatusAsync(cancellationToken);
        var sidecar = await sidecarStore.ReadAsync(cancellationToken);
        var entries = sidecar.HooksEntriesFor(status.HooksJsonPath);

        return Evaluate(
            status.HooksJsonPath,
            status.HooksEvents,
            eventName => entries.TryGetValue(eventName, out var entry) ? entry.Command : null,
            "nitro agent hooks copilot install",
            "nitro agent hooks copilot uninstall");
    }

    private static DoctorAgentCommand.HookHarnessDoctorResult? Evaluate(
        string path,
        IReadOnlyList<HookStatusEventResult> events,
        Func<string, string?> sidecarCommand,
        string installCommand,
        string uninstallCommand)
    {
        var anyPresent = events.Any(e => e.Outcome != HookStatusOutcome.Missing);
        var anySidecarRecord = events.Any(e => sidecarCommand(e.Event) is not null);

        if (!anyPresent && !anySidecarRecord)
        {
            // Never installed here: opting out is not a doctor finding.
            return null;
        }

        var issues = new List<string>();

        foreach (var eventResult in events)
        {
            var recordedCommand = sidecarCommand(eventResult.Event);

            if (eventResult.Outcome == HookStatusOutcome.Missing)
            {
                if (recordedCommand is not null)
                {
                    issues.Add(
                        $"'{eventResult.Event}': the sidecar records an installed entry, but it is "
                        + $"no longer present in the config (removed outside `{uninstallCommand}`?).");
                }

                continue;
            }

            if (recordedCommand is null)
            {
                issues.Add(
                    $"'{eventResult.Event}': an entry is present but has no matching sidecar record "
                    + "(a lost or corrupted sidecar, or installed outside nitro).");
            }
            else if (recordedCommand != eventResult.InstalledCommand)
            {
                issues.Add(
                    $"'{eventResult.Event}': the entry does not match nitro's sidecar record "
                    + "(hand-edited?).");
            }

            if (eventResult.Outcome == HookStatusOutcome.Outdated)
            {
                issues.Add($"'{eventResult.Event}': outdated; rerun `{installCommand}` to refresh it.");
            }
        }

        return new DoctorAgentCommand.HookHarnessDoctorResult(
            path,
            [.. events.Select(e => new DoctorAgentCommand.HookEventDoctorResult(e.Event, e.Outcome.ToString()))],
            issues.Count == 0,
            issues);
    }
}
