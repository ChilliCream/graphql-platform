using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CodexConfigTomlNotifyEditor"/> against golden
/// "before" <c>config.toml</c> fixtures under
/// <c>test/fixtures/hooks/codex/config-toml/</c>: the wrap-a-foreign-program
/// cycle, the restore-on-uninstall cycle, idempotent reinstall, a
/// <c>notify</c> key living inside an unrelated table (must not be touched),
/// and the safe-refusal path for a shape this narrow editor cannot confidently
/// parse.
/// </summary>
public sealed class CodexConfigTomlNotifyEditorTests
{
    private static readonly IReadOnlyList<string> s_ourArgv =
        ["/home/agent/.dotnet/tools/nitro", "agent", "hook", "codex", "notify"];

    [Fact]
    public void Install_MissingFile_InsertsOurNotifyLineWithNoPriorForeign()
    {
        var result = CodexConfigTomlNotifyEditor.Install(null, s_ourArgv, null, null);

        Assert.Equal(HookInstallOutcome.Installed, result.Outcome);
        Assert.Null(result.NewPriorForeign);
        Assert.Contains("notify = [\"/home/agent/.dotnet/tools/nitro\", \"agent\", \"hook\", \"codex\", \"notify\"]", result.ConfigToml);
    }

    [Fact]
    public void Install_ForeignNotify_WrapsItAndCapturesItAsPriorForeign()
    {
        var before = CodexHooksInstallFixtures.Read("config-toml", "foreign-notify.toml");

        var result = CodexConfigTomlNotifyEditor.Install(before, s_ourArgv, recordedOurArgv: null, recordedPriorForeign: null);

        Assert.Equal(HookInstallOutcome.Updated, result.Outcome);
        Assert.Equal(["/usr/local/bin/herdr-notify", "--flag"], result.NewPriorForeign);

        // Everything else in the file (model, [hooks.state] table) survives untouched.
        Assert.Contains("model = \"gpt-5.6-sol\"", result.ConfigToml);
        Assert.Contains("[hooks.state]", result.ConfigToml);
        Assert.DoesNotContain("herdr-notify", result.ConfigToml);
        Assert.Contains("agent\", \"hook\", \"codex\", \"notify\"", result.ConfigToml);
    }

    [Fact]
    public void Install_Twice_IsIdempotent_And_KeepsThePriorForeignRecord()
    {
        var before = CodexHooksInstallFixtures.Read("config-toml", "foreign-notify.toml");
        var first = CodexConfigTomlNotifyEditor.Install(before, s_ourArgv, null, null);
        Assert.Equal(HookInstallOutcome.Updated, first.Outcome);

        var second = CodexConfigTomlNotifyEditor.Install(
            first.ConfigToml, s_ourArgv, recordedOurArgv: s_ourArgv, recordedPriorForeign: first.NewPriorForeign);

        Assert.Equal(HookInstallOutcome.Unchanged, second.Outcome);
        Assert.Equal(first.ConfigToml, second.ConfigToml);
        Assert.Equal(first.NewPriorForeign, second.NewPriorForeign);
    }

    [Fact]
    public void Install_OurOwnStaleEntry_ReplacesItAndCarriesThePriorForeignRecordForward()
    {
        // What's on disk is our own previous argv, not a foreign program, so the sidecar's earlier
        // foreign record must carry forward untouched instead of being overwritten with our stale entry.
        var staleArgv = new List<string> { "/opt/old/nitro", "agent", "hook", "codex", "notify" };
        var before = $"notify = [{string.Join(", ", staleArgv.Select(a => $"\"{a}\""))}]\n";
        var recordedPriorForeign = new List<string> { "/usr/local/bin/herdr-notify" };

        var result = CodexConfigTomlNotifyEditor.Install(before, s_ourArgv, staleArgv, recordedPriorForeign);

        Assert.Equal(HookInstallOutcome.Updated, result.Outcome);
        Assert.Equal(recordedPriorForeign, result.NewPriorForeign);
        Assert.DoesNotContain("/opt/old/nitro", result.ConfigToml);
    }

    [Fact]
    public void Install_NotifyInsideAnotherTable_IsIgnored_TopLevelNotifyIsInsertedSeparately()
    {
        var before = CodexHooksInstallFixtures.Read("config-toml", "notify-in-table.toml");

        var result = CodexConfigTomlNotifyEditor.Install(before, s_ourArgv, null, null);

        Assert.Equal(HookInstallOutcome.Installed, result.Outcome);
        Assert.Null(result.NewPriorForeign);

        // The table's own notify key survives untouched; ours is a separate,
        // top-level key.
        Assert.Contains("[some.table]\nnotify = [\"not-top-level\"]", result.ConfigToml);
        Assert.Contains("agent\", \"hook\", \"codex\", \"notify\"", result.ConfigToml);
    }

    [Fact]
    public void Install_MultilineForeignArray_ThrowsRatherThanRisksMisparsing()
    {
        var before = CodexHooksInstallFixtures.Read("config-toml", "multiline-notify.toml");

        var exception = Assert.Throws<ExitException>(
            () => CodexConfigTomlNotifyEditor.Install(before, s_ourArgv, null, null));

        Assert.Contains("safely parse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Status_ReportsMissingInstalledAndOutdated()
    {
        Assert.Equal(HookStatusOutcome.Missing, CodexConfigTomlNotifyEditor.Status(null, s_ourArgv));

        var installed = $"notify = [{string.Join(", ", s_ourArgv.Select(a => $"\"{a}\""))}]\n";
        Assert.Equal(HookStatusOutcome.Installed, CodexConfigTomlNotifyEditor.Status(installed, s_ourArgv));

        var foreign = CodexHooksInstallFixtures.Read("config-toml", "foreign-notify.toml");
        Assert.Equal(HookStatusOutcome.Outdated, CodexConfigTomlNotifyEditor.Status(foreign, s_ourArgv));
    }

    [Fact]
    public void Uninstall_RestoresThePriorForeignProgramVerbatim()
    {
        var installed = $"model = \"gpt-5.6-sol\"\nnotify = [{string.Join(", ", s_ourArgv.Select(a => $"\"{a}\""))}]\n\n[hooks.state]\n";
        var recordedPriorForeign = new List<string> { "/usr/local/bin/herdr-notify", "--flag" };

        var result = CodexConfigTomlNotifyEditor.Uninstall(installed, s_ourArgv, recordedPriorForeign);

        Assert.Equal(HookUninstallOutcome.Removed, result.Outcome);
        Assert.Contains("notify = [\"/usr/local/bin/herdr-notify\", \"--flag\"]", result.ConfigToml);
        Assert.DoesNotContain("agent hook codex notify", result.ConfigToml);
        Assert.Contains("model = \"gpt-5.6-sol\"", result.ConfigToml);
        Assert.Contains("[hooks.state]", result.ConfigToml);
    }

    [Fact]
    public void Uninstall_NoPriorForeign_RemovesTheKeyEntirely()
    {
        var installed = $"model = \"gpt-5.6-sol\"\nnotify = [{string.Join(", ", s_ourArgv.Select(a => $"\"{a}\""))}]\n";

        var result = CodexConfigTomlNotifyEditor.Uninstall(installed, s_ourArgv, null);

        Assert.Equal(HookUninstallOutcome.Removed, result.Outcome);
        Assert.DoesNotContain("notify", result.ConfigToml);
        Assert.Contains("model = \"gpt-5.6-sol\"", result.ConfigToml);
    }

    [Fact]
    public void Uninstall_ForeignEditSinceInstall_LeavesItUntouched()
    {
        // A foreign edit landed since install, so the value on disk no longer matches and must not be clobbered.
        const string edited = "notify = [\"/something/else\"]\n";

        var result = CodexConfigTomlNotifyEditor.Uninstall(edited, s_ourArgv, ["/usr/local/bin/herdr-notify"]);

        Assert.Equal(HookUninstallOutcome.NotPresent, result.Outcome);
        Assert.Equal(edited, result.ConfigToml);
    }

    [Fact]
    public void Uninstall_MissingFile_NotPresent()
    {
        var result = CodexConfigTomlNotifyEditor.Uninstall(null, s_ourArgv, null);

        Assert.Equal(HookUninstallOutcome.NotPresent, result.Outcome);
    }
}
