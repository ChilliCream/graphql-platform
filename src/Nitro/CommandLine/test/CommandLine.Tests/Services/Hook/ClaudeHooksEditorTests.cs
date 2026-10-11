using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Tests <see cref="ClaudeHooksEditor"/> installation, status, and removal
/// against settings fixtures with Nitro-owned, foreign, and modified entries.
/// </summary>
public sealed class ClaudeHooksEditorTests
{
    private static readonly ClaudeHooksTemplate s_template = ClaudeHooksTemplate.Claude;

    private static readonly LaunchDescriptor s_descriptor =
        new("/home/agent/.dotnet/tools/nitro", []);

    [Fact]
    public void Install_Should_CreateAllFiveEventsAsInstalled_When_TheSettingsFileIsMissing()
    {
        // arrange
        // act
        var result = ClaudeHooksEditor.Install(null, s_template, s_descriptor, DateTimeOffset.UnixEpoch);

        // assert
        result.Outcomes.MatchInlineSnapshot(
            """
            [
              {
                "Event": "SessionStart",
                "Outcome": "Installed"
              },
              {
                "Event": "UserPromptSubmit",
                "Outcome": "Installed"
              },
              {
                "Event": "Stop",
                "Outcome": "Installed"
              },
              {
                "Event": "Notification",
                "Outcome": "Installed"
              },
              {
                "Event": "SessionEnd",
                "Outcome": "Installed"
              }
            ]
            """);

        var root = Parse(result.SettingsJson);
        var hooks = (JsonObject)root["hooks"]!;

        foreach (var claudeEvent in ClaudeHooksTemplate.Events)
        {
            var group = SingleGroup(hooks, claudeEvent);
            AssertCommand(group, s_template.BuildCommand(s_descriptor, claudeEvent), 10);
        }

        Assert.Equal(5, result.Sidecar.Count);
        Assert.All(result.Sidecar.Values, e => Assert.Equal(DateTimeOffset.UnixEpoch, e.InstalledAt));
    }

    [Fact]
    public void Install_ForeignOnly_AddsAlongsideWithoutTouchingForeignEntryOrOtherTopLevelKeys()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "foreign-only.json");
        var beforeRoot = Parse(before);
        var herdrBefore = ((JsonObject)beforeRoot["hooks"]!)["SessionStart"]![0];

        var result = ClaudeHooksEditor.Install(before, s_template, s_descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Installed, o.Outcome));

        var afterRoot = Parse(result.SettingsJson);
        var sessionStart = (JsonArray)((JsonObject)afterRoot["hooks"]!)["SessionStart"]!;

        Assert.Equal(2, sessionStart.Count);
        Assert.True(JsonNode.DeepEquals(herdrBefore, sessionStart[0]));
        AssertCommand(
            (JsonObject)sessionStart[1]!, s_template.BuildCommand(s_descriptor, "SessionStart"), 10);

        // Untouched foreign top-level content round-trips unchanged.
        Assert.True(JsonNode.DeepEquals(beforeRoot["someOtherTopLevelKey"], afterRoot["someOtherTopLevelKey"]));
    }

    [Fact]
    public void Install_Mixed_UpdatesOutdatedAddsMissingLeavesForeignAndCurrentAlone()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "mixed.json");
        var beforeRoot = Parse(before);
        var beforeHooks = (JsonObject)beforeRoot["hooks"]!;
        var herdrBefore = ((JsonArray)beforeHooks["SessionStart"]!)[0];
        var foreignSessionEndBefore = ((JsonArray)beforeHooks["SessionEnd"]!)[0];

        var result = ClaudeHooksEditor.Install(before, s_template, s_descriptor, DateTimeOffset.UnixEpoch);

        var byEvent = result.Outcomes.ToDictionary(o => o.Event, o => o.Outcome);
        Assert.Equal(HookInstallOutcome.Updated, byEvent["SessionStart"]);
        Assert.Equal(HookInstallOutcome.Unchanged, byEvent["UserPromptSubmit"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["Stop"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["SessionEnd"]);

        var afterHooks = (JsonObject)Parse(result.SettingsJson)["hooks"]!;

        var sessionStart = (JsonArray)afterHooks["SessionStart"]!;
        Assert.Equal(2, sessionStart.Count);
        Assert.True(JsonNode.DeepEquals(herdrBefore, sessionStart[0]));
        AssertCommand(
            (JsonObject)sessionStart[1]!, s_template.BuildCommand(s_descriptor, "SessionStart"), 10);

        var sessionEnd = (JsonArray)afterHooks["SessionEnd"]!;
        Assert.Equal(2, sessionEnd.Count);
        Assert.True(JsonNode.DeepEquals(foreignSessionEndBefore, sessionEnd[0]));
        AssertCommand(
            (JsonObject)sessionEnd[1]!, s_template.BuildCommand(s_descriptor, "SessionEnd"), 10);

        AssertCommand(
            SingleGroup(afterHooks, "UserPromptSubmit"),
            s_template.BuildCommand(s_descriptor, "UserPromptSubmit"),
            10);
        AssertCommand(SingleGroup(afterHooks, "Stop"), s_template.BuildCommand(s_descriptor, "Stop"), 10);
    }

    [Fact]
    public void Install_AlreadyInstalled_AllUnchanged()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "already-installed.json");

        var result = ClaudeHooksEditor.Install(before, s_template, s_descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Unchanged, o.Outcome));
        Assert.True(JsonNode.DeepEquals(Parse(before), Parse(result.SettingsJson)));
    }

    [Fact]
    public void Install_Outdated_ReplacesStaleCommandOnlyForThatEvent()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "outdated.json");

        var result = ClaudeHooksEditor.Install(before, s_template, s_descriptor, DateTimeOffset.UnixEpoch);

        var byEvent = result.Outcomes.ToDictionary(o => o.Event, o => o.Outcome);
        Assert.Equal(HookInstallOutcome.Updated, byEvent["SessionStart"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["UserPromptSubmit"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["Stop"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["SessionEnd"]);

        var hooks = (JsonObject)Parse(result.SettingsJson)["hooks"]!;
        AssertCommand(
            SingleGroup(hooks, "SessionStart"), s_template.BuildCommand(s_descriptor, "SessionStart"), 10);
    }

    [Fact]
    public void Install_ManuallyEdited_TreatedIdenticallyToOutdated()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "manually-edited.json");

        var result = ClaudeHooksEditor.Install(before, s_template, s_descriptor, DateTimeOffset.UnixEpoch);

        var outcome = Assert.Single(result.Outcomes, o => o.Event == "SessionStart");
        Assert.Equal(HookInstallOutcome.Updated, outcome.Outcome);

        var hooks = (JsonObject)Parse(result.SettingsJson)["hooks"]!;
        AssertCommand(
            SingleGroup(hooks, "SessionStart"), s_template.BuildCommand(s_descriptor, "SessionStart"), 10);
    }

    [Fact]
    public void Install_Twice_IsIdempotent()
    {
        var once = ClaudeHooksEditor.Install(
            null, s_template, s_descriptor, DateTimeOffset.UnixEpoch).SettingsJson;
        var twice = ClaudeHooksEditor.Install(
            once, s_template, s_descriptor, DateTimeOffset.UnixEpoch).SettingsJson;

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Status_Should_ReportNotificationMissing_When_TheInstallHasFourEntries()
    {
        // arrange
        var before = ClaudeHooksInstallFixtures.Read("install", "four-events.json");

        // act
        var result = ClaudeHooksEditor.Status(before, s_template, s_descriptor);

        // assert
        result.MatchInlineSnapshot(
            """
            [
              {
                "Event": "SessionStart",
                "Outcome": "Installed",
                "InstalledCommand": "/home/agent/.dotnet/tools/nitro agent hook claude session-start"
              },
              {
                "Event": "UserPromptSubmit",
                "Outcome": "Installed",
                "InstalledCommand": "/home/agent/.dotnet/tools/nitro agent hook claude user-prompt-submit"
              },
              {
                "Event": "Stop",
                "Outcome": "Installed",
                "InstalledCommand": "/home/agent/.dotnet/tools/nitro agent hook claude stop"
              },
              {
                "Event": "Notification",
                "Outcome": "Missing",
                "InstalledCommand": null
              },
              {
                "Event": "SessionEnd",
                "Outcome": "Installed",
                "InstalledCommand": "/home/agent/.dotnet/tools/nitro agent hook claude session-end"
              }
            ]
            """);
    }

    [Fact]
    public void Install_Should_AddNotificationAndLeaveTheOtherFourUnchanged_When_TheInstallHasFourEntries()
    {
        // arrange
        var before = ClaudeHooksInstallFixtures.Read("install", "four-events.json");

        // act
        var result = ClaudeHooksEditor.Install(before, s_template, s_descriptor, DateTimeOffset.UnixEpoch);

        // assert
        result.Outcomes.MatchInlineSnapshot(
            """
            [
              {
                "Event": "SessionStart",
                "Outcome": "Unchanged"
              },
              {
                "Event": "UserPromptSubmit",
                "Outcome": "Unchanged"
              },
              {
                "Event": "Stop",
                "Outcome": "Unchanged"
              },
              {
                "Event": "Notification",
                "Outcome": "Installed"
              },
              {
                "Event": "SessionEnd",
                "Outcome": "Unchanged"
              }
            ]
            """);
        ClaudeHooksEditor.Status(result.SettingsJson, s_template, s_descriptor).MatchInlineSnapshot(
            """
            [
              {
                "Event": "SessionStart",
                "Outcome": "Installed",
                "InstalledCommand": "/home/agent/.dotnet/tools/nitro agent hook claude session-start"
              },
              {
                "Event": "UserPromptSubmit",
                "Outcome": "Installed",
                "InstalledCommand": "/home/agent/.dotnet/tools/nitro agent hook claude user-prompt-submit"
              },
              {
                "Event": "Stop",
                "Outcome": "Installed",
                "InstalledCommand": "/home/agent/.dotnet/tools/nitro agent hook claude stop"
              },
              {
                "Event": "Notification",
                "Outcome": "Installed",
                "InstalledCommand": "/home/agent/.dotnet/tools/nitro agent hook claude notification"
              },
              {
                "Event": "SessionEnd",
                "Outcome": "Installed",
                "InstalledCommand": "/home/agent/.dotnet/tools/nitro agent hook claude session-end"
              }
            ]
            """);
    }

    [Fact]
    public void Status_ReportsMissingInstalledAndOutdated()
    {
        var missing = ClaudeHooksEditor.Status(null, s_template, s_descriptor);
        Assert.All(missing, r => Assert.Equal(HookStatusOutcome.Missing, r.Outcome));

        var installed = ClaudeHooksEditor.Status(
            ClaudeHooksInstallFixtures.Read("install", "already-installed.json"), s_template, s_descriptor);
        Assert.All(installed, r => Assert.Equal(HookStatusOutcome.Installed, r.Outcome));

        var outdated = ClaudeHooksEditor.Status(
            ClaudeHooksInstallFixtures.Read("install", "outdated.json"), s_template, s_descriptor);
        var sessionStart = Assert.Single(outdated, r => r.Event == "SessionStart");
        Assert.Equal(HookStatusOutcome.Outdated, sessionStart.Outcome);
        Assert.Equal("/opt/old/nitro agent hook claude session-start", sessionStart.InstalledCommand);
        Assert.All(outdated.Where(r => r.Event != "SessionStart"), r => Assert.Equal(HookStatusOutcome.Missing, r.Outcome));

        var manuallyEdited = ClaudeHooksEditor.Status(
            ClaudeHooksInstallFixtures.Read("install", "manually-edited.json"), s_template, s_descriptor);
        Assert.Equal(HookStatusOutcome.Outdated, Assert.Single(manuallyEdited, r => r.Event == "SessionStart").Outcome);
    }

    [Fact]
    public void Uninstall_MissingFile_AllNotPresent()
    {
        var result = ClaudeHooksEditor.Uninstall(
            null, s_template, new Dictionary<string, ClaudeHooksSidecarEntry>());

        Assert.All(result.Outcomes, o => Assert.Equal(HookUninstallOutcome.NotPresent, o.Outcome));
        Assert.Empty(result.Sidecar);
    }

    [Fact]
    public void Uninstall_WithForeignEntry_RemovesOnlyOwnAndLeavesForeignEntryByteForByte()
    {
        var before = ClaudeHooksInstallFixtures.Read("uninstall", "with-foreign.json");
        var beforeRoot = Parse(before);
        var beforeHooks = (JsonObject)beforeRoot["hooks"]!;
        var herdrBefore = ((JsonArray)beforeHooks["SessionStart"]!)[0];

        var sidecar = SidecarFor(before);

        var result = ClaudeHooksEditor.Uninstall(before, s_template, sidecar);

        Assert.All(result.Outcomes, o => Assert.Equal(HookUninstallOutcome.Removed, o.Outcome));
        Assert.Empty(result.Sidecar);

        var afterRoot = Parse(result.SettingsJson);
        var afterHooks = (JsonObject)afterRoot["hooks"]!;

        // Only the herdr-owning event survives, and only the herdr group in it.
        Assert.Equal(["SessionStart"], afterHooks.Select(kv => kv.Key));
        var sessionStart = (JsonArray)afterHooks["SessionStart"]!;
        var herdrAfter = Assert.Single(sessionStart);

        Assert.True(JsonNode.DeepEquals(herdrBefore, herdrAfter));
        Assert.Equal(herdrBefore!.ToJsonString(), herdrAfter!.ToJsonString());
    }

    [Fact]
    public void Uninstall_SidecarDrifted_FallsBackToMarkerMatch()
    {
        var before = ClaudeHooksInstallFixtures.Read("uninstall", "drifted.json");

        // The fixture retains the ownership marker but differs from the recorded install command.
        var staleSidecar = new Dictionary<string, ClaudeHooksSidecarEntry>
        {
            ["SessionStart"] = new(
                s_template.BuildCommand(s_descriptor, "SessionStart"),
                10,
                ClaudeHooksSidecarEntry.ComputeHash(s_template.BuildCommand(s_descriptor, "SessionStart"), 10),
                DateTimeOffset.UnixEpoch)
        };

        var result = ClaudeHooksEditor.Uninstall(before, s_template, staleSidecar);

        var sessionStart = Assert.Single(result.Outcomes, o => o.Event == "SessionStart");
        Assert.Equal(HookUninstallOutcome.Removed, sessionStart.Outcome);

        var root = Parse(result.SettingsJson);
        Assert.Null(root["hooks"]);
    }

    [Fact]
    public void Uninstall_ForeignSessionStartWithNoOwnedGroup_LeavesItInPlace()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "foreign-only.json");

        var result = ClaudeHooksEditor.Uninstall(
            before, s_template, new Dictionary<string, ClaudeHooksSidecarEntry>());

        var sessionStart = Assert.Single(result.Outcomes, o => o.Event == "SessionStart");
        Assert.Equal(HookUninstallOutcome.NotPresent, sessionStart.Outcome);
        Assert.True(JsonNode.DeepEquals(Parse(before), Parse(result.SettingsJson)));
    }

    [Fact]
    public void Install_Should_UpdateTheOwnedHandlerInPlaceAndDropDuplicates_When_ItSharesAGroupWithAUserHook()
    {
        // arrange
        var settings =
            $$"""
            {
              "hooks": {
                "Stop": [
                  {
                    "matcher": "*",
                    "hooks": [
                      { "type": "command", "command": "./mine.sh" },
                      { "type": "command", "command": "/opt/old/nitro agent hook venti stop", "timeout": 5 }
                    ]
                  },
                  { "hooks": [{ "type": "command", "command": "{{VentiCommand("Stop")}}", "timeout": 10 }] }
                ]
              }
            }
            """;

        // act
        var result = ClaudeHooksEditor.Install(settings, ClaudeHooksTemplate.Venti, s_descriptor, DateTimeOffset.UnixEpoch);

        // assert
        Assert.Equal(HookInstallOutcome.Updated, Assert.Single(result.Outcomes, o => o.Event == "Stop").Outcome);
        Format(Parse(result.SettingsJson)["hooks"]!["Stop"]!).MatchInlineSnapshot(
            """
            [
              {
                "matcher": "*",
                "hooks": [
                  {
                    "type": "command",
                    "command": "./mine.sh"
                  },
                  {
                    "type": "command",
                    "command": "/home/agent/.dotnet/tools/nitro agent hook venti stop",
                    "timeout": 10
                  }
                ]
              }
            ]
            """);
    }

    [Fact]
    public void Install_Should_MoveTheOwnedHandlerToItsOwnGroup_When_AMatcherNarrowsItsGroup()
    {
        // arrange
        var settings =
            $$"""
            {
              "hooks": {
                "SessionStart": [
                  {
                    "matcher": "startup",
                    "hooks": [
                      { "type": "command", "command": "./mine.sh" },
                      { "type": "command", "command": "{{VentiCommand("SessionStart")}}", "timeout": 10 }
                    ]
                  }
                ]
              }
            }
            """;

        // act
        var result = ClaudeHooksEditor.Install(settings, ClaudeHooksTemplate.Venti, s_descriptor, DateTimeOffset.UnixEpoch);

        // assert
        Format(Parse(result.SettingsJson)["hooks"]!["SessionStart"]!).MatchInlineSnapshot(
            """
            [
              {
                "matcher": "startup",
                "hooks": [
                  {
                    "type": "command",
                    "command": "./mine.sh"
                  }
                ]
              },
              {
                "hooks": [
                  {
                    "type": "command",
                    "command": "/home/agent/.dotnet/tools/nitro agent hook venti session-start",
                    "timeout": 10
                  }
                ]
              }
            ]
            """);
    }

    [Theory]
    [InlineData("\"type\": \"command\", \"timeout\": 10, \"async\": true")]
    [InlineData("\"type\": \"command\", \"timeout\": 10, \"enabled\": \"no\"")]
    [InlineData("\"type\": \"command\", \"timeout\": 10, \"statusMessage\": 3")]
    [InlineData("\"type\": \"command\", \"timeout\": \"10\"")]
    [InlineData("\"type\": \"prompt\", \"timeout\": 10")]
    public void Install_Should_LeaveARunnableHandler_When_TheOwnedHandlerIsNotRunnableAsWritten(string properties)
    {
        // arrange
        var settings =
            $$"""
            {
              "hooks": {
                "Stop": [
                  { "hooks": [{ "command": "{{VentiCommand("Stop")}}", {{properties}} }] }
                ]
              }
            }
            """;
        var statusBefore = ClaudeHooksEditor.Status(settings, ClaudeHooksTemplate.Venti, s_descriptor);

        // act
        var result = ClaudeHooksEditor.Install(settings, ClaudeHooksTemplate.Venti, s_descriptor, DateTimeOffset.UnixEpoch);

        // assert
        var statusAfter = ClaudeHooksEditor.Status(result.SettingsJson, ClaudeHooksTemplate.Venti, s_descriptor);
        Assert.Equal(
            (HookStatusOutcome.Outdated, HookInstallOutcome.Updated, HookStatusOutcome.Installed),
            (statusBefore.Single(r => r.Event == "Stop").Outcome,
                result.Outcomes.Single(o => o.Event == "Stop").Outcome,
                statusAfter.Single(r => r.Event == "Stop").Outcome));
    }

    [Fact]
    public void Install_Should_MoveTheHandlerKeepingItSwitchedOff_When_ADisabledHandlerSharesANarrowedGroupWithUserHooks()
    {
        // arrange
        const string settings =
            """
            {
              "hooks": {
                "Notification": [
                  { "hooks": [{ "type": "command", "command": "./always.sh" }] },
                  {
                    "matcher": "idle_prompt",
                    "hooks": [
                      { "type": "command", "command": "./mine.sh" },
                      {
                        "type": "command",
                        "command": "/opt/old/nitro agent hook venti notification",
                        "timeout": 10,
                        "enabled": false,
                        "statusMessage": "Checking mail"
                      }
                    ]
                  }
                ]
              }
            }
            """;

        // act
        var result = ClaudeHooksEditor.Install(settings, ClaudeHooksTemplate.Venti, s_descriptor, DateTimeOffset.UnixEpoch);

        // assert
        Assert.Equal(
            (HookInstallOutcome.Disabled, HookStatusOutcome.Disabled),
            (result.Outcomes.Single(o => o.Event == "Notification").Outcome,
                ClaudeHooksEditor.Status(result.SettingsJson, ClaudeHooksTemplate.Venti, s_descriptor)
                    .Single(r => r.Event == "Notification").Outcome));
        Format(Parse(result.SettingsJson)["hooks"]!["Notification"]!).MatchInlineSnapshot(
            """
            [
              {
                "hooks": [
                  {
                    "type": "command",
                    "command": "./always.sh"
                  }
                ]
              },
              {
                "matcher": "idle_prompt",
                "hooks": [
                  {
                    "type": "command",
                    "command": "./mine.sh"
                  }
                ]
              },
              {
                "hooks": [
                  {
                    "type": "command",
                    "command": "/home/agent/.dotnet/tools/nitro agent hook venti notification",
                    "timeout": 10,
                    "enabled": false,
                    "statusMessage": "Checking mail"
                  }
                ]
              }
            ]
            """);
    }

    [Theory]
    [InlineData("\"matcher\": null, ", "")]
    [InlineData("", ", \"async\": null")]
    [InlineData("", ", \"enabled\": null")]
    [InlineData("", ", \"statusMessage\": null")]
    public void Install_Should_RepairTheHandler_When_AnOptionalPropertyIsExplicitlyNull(
        string groupProperties,
        string handlerProperties)
    {
        // arrange
        var settings =
            $$"""
            {
              "hooks": {
                "Stop": [
                  { {{groupProperties}}"hooks": [{ "type": "command", "command": "{{VentiCommand("Stop")}}", "timeout": 10{{handlerProperties}} }] }
                ]
              }
            }
            """;
        var statusBefore = ClaudeHooksEditor.Status(settings, ClaudeHooksTemplate.Venti, s_descriptor);

        // act
        var result = ClaudeHooksEditor.Install(settings, ClaudeHooksTemplate.Venti, s_descriptor, DateTimeOffset.UnixEpoch);

        // assert
        Assert.Equal(
            (HookStatusOutcome.Outdated, HookInstallOutcome.Updated, HookStatusOutcome.Installed),
            (statusBefore.Single(r => r.Event == "Stop").Outcome,
                result.Outcomes.Single(o => o.Event == "Stop").Outcome,
                ClaudeHooksEditor.Status(result.SettingsJson, ClaudeHooksTemplate.Venti, s_descriptor)
                    .Single(r => r.Event == "Stop").Outcome));
        Format(Parse(result.SettingsJson)["hooks"]!["Stop"]!).MatchInlineSnapshot(
            """
            [
              {
                "hooks": [
                  {
                    "type": "command",
                    "command": "/home/agent/.dotnet/tools/nitro agent hook venti stop",
                    "timeout": 10
                  }
                ]
              }
            ]
            """);
    }

    [Fact]
    public void Status_Should_ReportOutdated_When_TheEventHasTwoOwnedHandlers()
    {
        // arrange
        var command = VentiCommand("Stop");
        var settings =
            $$"""
            {
              "hooks": {
                "Stop": [
                  { "hooks": [{ "type": "command", "command": "{{command}}", "timeout": 10 }] },
                  { "hooks": [{ "type": "command", "command": "{{command}}", "timeout": 10 }] }
                ]
              }
            }
            """;

        // act
        var status = ClaudeHooksEditor.Status(settings, ClaudeHooksTemplate.Venti, s_descriptor);

        // assert
        Assert.Equal(HookStatusOutcome.Outdated, status.Single(r => r.Event == "Stop").Outcome);
    }

    [Fact]
    public void Install_Should_KeepTheEnabledFlagAndReportDisabled_When_TheVentiHandlerIsSwitchedOff()
    {
        // arrange
        const string settings =
            """
            {
              "hooks": {
                "Stop": [
                  { "hooks": [{ "type": "command", "command": "/opt/old/nitro agent hook venti stop", "timeout": 10, "enabled": false }] }
                ]
              }
            }
            """;

        // act
        var result = ClaudeHooksEditor.Install(settings, ClaudeHooksTemplate.Venti, s_descriptor, DateTimeOffset.UnixEpoch);

        // assert
        Assert.Equal(
            (HookInstallOutcome.Disabled, HookStatusOutcome.Disabled),
            (result.Outcomes.Single(o => o.Event == "Stop").Outcome,
                ClaudeHooksEditor.Status(result.SettingsJson, ClaudeHooksTemplate.Venti, s_descriptor)
                    .Single(r => r.Event == "Stop").Outcome));
        Format(Parse(result.SettingsJson)["hooks"]!["Stop"]!).MatchInlineSnapshot(
            """
            [
              {
                "hooks": [
                  {
                    "type": "command",
                    "command": "/home/agent/.dotnet/tools/nitro agent hook venti stop",
                    "timeout": 10,
                    "enabled": false
                  }
                ]
              }
            ]
            """);
    }

    [Fact]
    public void Status_Should_ReportInstalled_When_AClaudeHandlerCarriesAnEnabledFlag()
    {
        // arrange
        var settings =
            $$"""
            {
              "hooks": {
                "Stop": [
                  { "hooks": [{ "type": "command", "command": "{{s_template.BuildCommand(s_descriptor, "Stop")}}", "timeout": 10, "enabled": false }] }
                ]
              }
            }
            """;

        // act
        var status = ClaudeHooksEditor.Status(settings, s_template, s_descriptor);

        // assert
        Assert.Equal(HookStatusOutcome.Installed, status.Single(r => r.Event == "Stop").Outcome);
    }

    [Fact]
    public void Uninstall_Should_RemoveEveryOwnedHandlerAndKeepSiblingsWithTheirGroup_When_OwnedHandlersShareGroups()
    {
        // arrange
        var command = VentiCommand("Stop");
        var settings =
            $$"""
            {
              "hooks": {
                "Stop": [
                  {
                    "matcher": "*",
                    "hooks": [
                      { "type": "command", "command": "{{command}}", "timeout": 10 },
                      { "type": "command", "command": "./mine.sh" }
                    ]
                  },
                  { "hooks": [{ "type": "command", "command": "{{command}}", "timeout": 10 }] },
                  { "hooks": [{ "type": "command", "command": "/opt/old/nitro agent hook venti stop" }] }
                ]
              }
            }
            """;

        // act
        var result = ClaudeHooksEditor.Uninstall(
            settings, ClaudeHooksTemplate.Venti, new Dictionary<string, ClaudeHooksSidecarEntry>());

        // assert
        Assert.Equal(HookUninstallOutcome.Removed, result.Outcomes.Single(o => o.Event == "Stop").Outcome);
        Format(Parse(result.SettingsJson)).MatchInlineSnapshot(
            """
            {
              "hooks": {
                "Stop": [
                  {
                    "matcher": "*",
                    "hooks": [
                      {
                        "type": "command",
                        "command": "./mine.sh"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    private static string VentiCommand(string hookEvent)
        => ClaudeHooksTemplate.Venti.BuildCommand(s_descriptor, hookEvent);

    private static Dictionary<string, ClaudeHooksSidecarEntry> SidecarFor(string installedJson)
    {
        var hooks = (JsonObject)Parse(installedJson)["hooks"]!;
        var sidecar = new Dictionary<string, ClaudeHooksSidecarEntry>();

        foreach (var claudeEvent in ClaudeHooksTemplate.Events)
        {
            var group = SingleOwnedGroup(hooks, claudeEvent);
            var hook = (JsonObject)((JsonArray)group["hooks"]!)[0]!;
            var command = hook["command"]!.GetValue<string>();
            var timeout = hook["timeout"]!.GetValue<int>();

            sidecar[claudeEvent] = new ClaudeHooksSidecarEntry(
                command, timeout, ClaudeHooksSidecarEntry.ComputeHash(command, timeout), DateTimeOffset.UnixEpoch);
        }

        return sidecar;
    }

    private static JsonObject SingleOwnedGroup(JsonObject hooks, string claudeEvent)
        => (JsonObject)((JsonArray)hooks[claudeEvent]!)
            .Single(g => ((JsonObject)g!)["hooks"]!.AsArray()
                .All(h => ((JsonObject)h!)["command"]!.GetValue<string>()
                    .Contains(s_template.CommandMarker, StringComparison.Ordinal)))!;

    private static JsonObject SingleGroup(JsonObject hooks, string claudeEvent)
    {
        var array = (JsonArray)hooks[claudeEvent]!;
        return (JsonObject)Assert.Single(array)!;
    }

    private static void AssertCommand(JsonObject group, string expectedCommand, int expectedTimeout)
    {
        var hooksArray = (JsonArray)group["hooks"]!;
        var hook = (JsonObject)Assert.Single(hooksArray)!;

        Assert.Equal("command", hook["type"]!.GetValue<string>());
        Assert.Equal(expectedCommand, hook["command"]!.GetValue<string>());
        Assert.Equal(expectedTimeout, hook["timeout"]!.GetValue<int>());
    }

    private static JsonObject Parse(string json) => (JsonObject)JsonNode.Parse(json)!;

    private static string Format(JsonNode node) => node.ToJsonString(new() { WriteIndented = true });
}
