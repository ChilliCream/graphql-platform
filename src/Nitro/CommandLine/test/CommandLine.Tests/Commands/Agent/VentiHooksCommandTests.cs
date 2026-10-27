using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <c>nitro agent hooks venti</c> through the real command pipeline against a project-scoped
/// <c>.venti/settings.json</c> in a temporary workspace, with the user-scope
/// <c>~/.venti/settings.json</c> redirected into the temporary tree.
/// </summary>
public sealed partial class VentiHooksCommandTests : AgentCommandTestBase
{
    private const string UserSettings =
        """
        {
          "model": "fake-model",
          "hooks": {
            "Stop": [
              {
                "hooks": [
                  { "type": "command", "command": "./scripts/notify.sh", "timeout": 5 }
                ]
              }
            ],
            "PreToolUse": [
              {
                "matcher": "Bash",
                "hooks": [
                  { "type": "command", "command": "./scripts/check-command.sh" }
                ]
              }
            ]
          }
        }
        """;

    public VentiHooksCommandTests(NitroCommandFixture fixture) : base(fixture)
    {
        SetupVentiSettingsPathResolver(UserSettingsPath, SettingsPath);
    }

    private string SettingsPath => Path.Combine(WorkingDirectory, ".venti", "settings.json");

    private string UserSettingsPath
        => Path.GetFullPath(Path.Combine(WorkingDirectory, "..", "home", ".venti", "settings.json"));

    [Fact]
    public async Task Install_Should_AddNativeEntriesBesideUserHooks_When_ProjectScopeIsSelected()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        WriteSettings(UserSettings);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");

        // assert
        Assert.Equal(0, result.ExitCode);
        var settings = await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken);
        NormalizeCommands(settings).MatchInlineSnapshot(
            """
            {
              "model": "fake-model",
              "hooks": {
                "Stop": [
                  {
                    "hooks": [
                      {
                        "type": "command",
                        "command": "./scripts/notify.sh",
                        "timeout": 5
                      }
                    ]
                  },
                  {
                    "hooks": [
                      {
                        "type": "command",
                        "command": "<nitro> agent hook venti stop",
                        "timeout": 10
                      }
                    ]
                  }
                ],
                "PreToolUse": [
                  {
                    "matcher": "Bash",
                    "hooks": [
                      {
                        "type": "command",
                        "command": "./scripts/check-command.sh"
                      }
                    ]
                  }
                ],
                "AgentNaming": [
                  {
                    "matcher": "main",
                    "hooks": [
                      {
                        "type": "command",
                        "command": "<nitro> agent hook venti agent-naming",
                        "timeout": 10
                      }
                    ]
                  }
                ],
                "SessionStart": [
                  {
                    "hooks": [
                      {
                        "type": "command",
                        "command": "<nitro> agent hook venti session-start",
                        "timeout": 10
                      }
                    ]
                  }
                ],
                "UserPromptSubmit": [
                  {
                    "hooks": [
                      {
                        "type": "command",
                        "command": "<nitro> agent hook venti user-prompt-submit",
                        "timeout": 10
                      }
                    ]
                  }
                ],
                "Notification": [
                  {
                    "hooks": [
                      {
                        "type": "command",
                        "command": "<nitro> agent hook venti notification",
                        "timeout": 10
                      }
                    ]
                  }
                ],
                "SessionEnd": [
                  {
                    "hooks": [
                      {
                        "type": "command",
                        "command": "<nitro> agent hook venti session-end",
                        "timeout": 10
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Install_Should_ReportEveryEventUnchanged_When_RunTwice()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        WriteSettings(UserSettings);
        await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");
        var afterFirstInstall = await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");

        // assert
        Assert.Equal(
            afterFirstInstall,
            await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
        result.StdOut.Replace(SettingsPath, "<settings>").MatchInlineSnapshot(
            """
            ✓ Installed Venti hooks in '<settings>'.
              AgentNaming       unchanged
              SessionStart      unchanged
              UserPromptSubmit  unchanged
              Stop              unchanged
              Notification      unchanged
              SessionEnd        unchanged
            """);
    }

    [Fact]
    public async Task Status_Should_ReportCurrent_When_NativeEntriesAreInstalled()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "status", "--scope", "project");

        // assert
        Assert.Equal(0, result.ExitCode);
        result.StdOut.Replace(SettingsPath, "<settings>").MatchInlineSnapshot(
            """
            Venti hooks in '<settings>':
              AgentNaming       installed
              SessionStart      installed
              UserPromptSubmit  installed
              Stop              installed
              Notification      installed
              SessionEnd        installed
            """);
    }

    [Fact]
    public async Task Status_Should_ReportAgentNamingMissingAndFail_When_TheEntriesPredateIt()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");
        var settings = JsonNode.Parse(
            await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken))!;
        settings["hooks"]!.AsObject().Remove("AgentNaming");
        WriteSettings(settings.ToJsonString());

        // act
        var status = await ExecuteCommandAsync("agent", "hooks", "venti", "status", "--scope", "project");
        var install = await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");

        // assert
        Assert.Equal((1, 0), (status.ExitCode, install.ExitCode));
        new[] { status.StdOut, install.StdOut }
            .Select(output => output.Replace(SettingsPath, "<settings>"))
            .MatchInlineSnapshots(
            [
                """
                Venti hooks in '<settings>':
                  AgentNaming       missing
                  SessionStart      installed
                  UserPromptSubmit  installed
                  Stop              installed
                  Notification      installed
                  SessionEnd        installed
                """,
                """
                ✓ Installed Venti hooks in '<settings>'.
                  AgentNaming       installed
                  SessionStart      unchanged
                  UserPromptSubmit  unchanged
                  Stop              unchanged
                  Notification      unchanged
                  SessionEnd        unchanged
                """
            ]);
    }

    [Fact]
    public async Task Uninstall_Should_RestoreTheUserHooks_When_NativeEntriesWereInstalled()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        WriteSettings(UserSettings);
        await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "uninstall", "--scope", "project");

        // assert
        Assert.Equal(0, result.ExitCode);
        var settings = await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(UserSettings), JsonNode.Parse(settings)));
    }

    [Theory]
    [InlineData("install")]
    [InlineData("status")]
    public async Task Command_Should_FailWithoutWriting_When_TheSettingsRunNitroClaudeHooks(string verb)
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        const string settings =
            """
            {
              "hooks": {
                "Stop": [
                  {
                    "hooks": [
                      { "type": "command", "command": "./scripts/notify.sh" },
                      { "type": "command", "command": "nitro agent hook claude stop" }
                    ]
                  }
                ],
                "SessionEnd": [
                  { "hooks": [{ "type": "command", "command": "nitro agent hook claude session-end", "timeout": 10 }] }
                ]
              }
            }
            """;
        WriteSettings(settings);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", verb, "--scope", "project");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(settings, await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
        result.StdErr.Replace(SettingsPath, "<settings>").MatchInlineSnapshot(
            """
            '<settings>' already runs Nitro's `agent hook claude` hooks under Stop, SessionEnd. Next to the `agent hook venti` hooks in '<settings>' they would handle every event twice. Remove the `agent hook claude` entries from '<settings>', keeping any other hooks, then run the command again. Nothing was written.
            """);
    }

    [Theory]
    [InlineData("install")]
    [InlineData("status")]
    public async Task Command_Should_FailWithoutWritingEitherFile_When_TheUserSettingsRunNitroClaudeHooks(string verb)
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        const string userSettings =
            """
            {
              "hooks": {
                "SessionStart": [
                  { "hooks": [{ "type": "command", "command": "nitro agent hook claude session-start", "timeout": 10 }] }
                ]
              }
            }
            """;
        WriteFile(UserSettingsPath, userSettings);
        WriteSettings(UserSettings);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", verb, "--scope", "project");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            (userSettings, UserSettings),
            (await File.ReadAllTextAsync(UserSettingsPath, TestContext.Current.CancellationToken),
                await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken)));
        ReplacePaths(result.StdErr).MatchInlineSnapshot(
            """
            '<user-settings>' already runs Nitro's `agent hook claude` hooks under SessionStart. Next to the `agent hook venti` hooks in '<settings>' they would handle every event twice. Remove the `agent hook claude` entries from '<user-settings>', keeping any other hooks, then run the command again. Nothing was written.
            """);
    }

    [Fact]
    public async Task Install_Should_FailWithoutWriting_When_TheLaunchDirectorySettingsRunNitroClaudeHooks()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        const string projectSettings =
            """
            {
              "hooks": {
                "Stop": [
                  {
                    "hooks": [
                      { "type": "command", "command": "./mine.sh" },
                      { "type": "command", "command": "nitro agent hook claude stop" }
                    ]
                  }
                ]
              }
            }
            """;
        WriteSettings(projectSettings);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "user");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            (false, projectSettings),
            (File.Exists(UserSettingsPath),
                await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken)));
        ReplacePaths(result.StdErr).MatchInlineSnapshot(
            """
            '<settings>' already runs Nitro's `agent hook claude` hooks under Stop. Next to the `agent hook venti` hooks in '<user-settings>' they would handle every event twice. Remove the `agent hook claude` entries from '<settings>', keeping any other hooks, then run the command again. Nothing was written.
            """);
    }

    [Fact]
    public async Task Uninstall_Should_RemoveEveryNativeHandlerAndKeepTheUserHook_When_TheyShareAGroup()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");
        // a user hook joins the native Stop group and an older install appended a second native group
        var settings = JsonNode.Parse(await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken))!;
        var stopGroups = settings["hooks"]!["Stop"]!.AsArray();
        var nativeStop = stopGroups[0]!["hooks"]![0]!;
        stopGroups[0]!["hooks"]!.AsArray().Insert(0, new JsonObject { ["type"] = "command", ["command"] = "./mine.sh" });
        stopGroups[0]!["matcher"] = "*";
        stopGroups.Add(new JsonObject { ["hooks"] = new JsonArray(nativeStop.DeepClone()) });
        WriteSettings(settings.ToJsonString());

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "uninstall", "--scope", "project");

        // assert
        Assert.Equal(0, result.ExitCode);
        (await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken)).MatchInlineSnapshot(
            """
            {
              "hooks": {
                "Stop": [
                  {
                    "hooks": [
                      {
                        "type": "command",
                        "command": "./mine.sh"
                      }
                    ],
                    "matcher": "*"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Install_Should_ReportStopUnchanged_When_TheNativeHandlerSharesAGroupWithAUserHook()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");
        var settings = JsonNode.Parse(await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken))!;
        settings["hooks"]!["Stop"]![0]!["hooks"]!.AsArray()
            .Insert(0, new JsonObject { ["type"] = "command", ["command"] = "./mine.sh" });
        WriteSettings(settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var before = await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");

        // assert
        Assert.Equal(before, await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
        ReplacePaths(result.StdOut).MatchInlineSnapshot(
            """
            ✓ Installed Venti hooks in '<settings>'.
              AgentNaming       unchanged
              SessionStart      unchanged
              UserPromptSubmit  unchanged
              Stop              unchanged
              Notification      unchanged
              SessionEnd        unchanged
            """);
    }

    [Fact]
    public async Task Status_Should_ReportTheSwitchedOffHandlerAndFail_When_ItsEnabledFlagIsFalse()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");
        await SwitchOffStopHandlerAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "status", "--scope", "project");

        // assert
        Assert.Equal(1, result.ExitCode);
        ReplacePaths(result.StdOut).MatchInlineSnapshot(
            """
            Venti hooks in '<settings>':
              AgentNaming       installed
              SessionStart      installed
              UserPromptSubmit  installed
              Stop              disabled
              Notification      installed
              SessionEnd        installed
            Venti skips Nitro's Stop hooks because their entries in '<settings>' set "enabled": false. Remove that flag or set it to true to let them run.
            """);
    }

    [Fact]
    public async Task Install_Should_KeepTheHandlerSwitchedOffAndReportIt_When_ItsEnabledFlagIsFalse()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");
        await SwitchOffStopHandlerAsync();
        var before = await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");

        // assert
        Assert.Equal(
            (0, before),
            (result.ExitCode, await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken)));
        ReplacePaths(result.StdOut).MatchInlineSnapshot(
            """
            ✓ Installed Venti hooks in '<settings>'.
              AgentNaming       unchanged
              SessionStart      unchanged
              UserPromptSubmit  unchanged
              Stop              disabled
              Notification      unchanged
              SessionEnd        unchanged
            Venti skips Nitro's Stop hooks because their entries in '<settings>' set "enabled": false. Remove that flag or set it to true to let them run.
            """);
    }

    [Theory]
    [InlineData("install", 0)]
    [InlineData("status", 1)]
    public async Task Command_Should_ReportTheGlobalSwitchWithoutChangingIt_When_TheUserSettingsDisableAllHooks(
        string verb,
        int expectedExitCode)
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--scope", "project");
        const string userSettings = """{ "disableAllHooks": true }""";
        WriteFile(UserSettingsPath, userSettings);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", verb, "--scope", "project");

        // assert
        Assert.Equal(
            (expectedExitCode, userSettings),
            (result.ExitCode, await File.ReadAllTextAsync(UserSettingsPath, TestContext.Current.CancellationToken)));
        ReplacePaths(result.StdOut).Split('\n').Last().MatchInlineSnapshot(
            """
            Venti runs no hooks, Nitro's included, while '<user-settings>' sets "disableAllHooks": true. Set it to false or remove it to let them run.
            """);
    }

    [Fact]
    public async Task Commands_Should_WriteJsonResults_When_JsonOutputIsRequested()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();

        // act
        var install = await ExecuteCommandAsync(
            "agent", "hooks", "venti", "install", "--scope", "project", "--output", "json");
        var status = await ExecuteCommandAsync(
            "agent", "hooks", "venti", "status", "--scope", "project", "--output", "json");
        var uninstall = await ExecuteCommandAsync(
            "agent", "hooks", "venti", "uninstall", "--scope", "project", "--output", "json");

        // assert
        Assert.Equal((0, 0, 0), (install.ExitCode, status.ExitCode, uninstall.ExitCode));
        Assert.Equal("{}", NormalizeJson(await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken)));
        new[] { install.StdOut, status.StdOut, uninstall.StdOut }
            .Select(output => NormalizeCommands(ReplacePaths(NormalizeJson(output))))
            .MatchInlineSnapshots(
            [
                """
                {
                  "settingsPath": "<settings>",
                  "events": [
                    {
                      "event": "AgentNaming",
                      "outcome": "Installed"
                    },
                    {
                      "event": "SessionStart",
                      "outcome": "Installed"
                    },
                    {
                      "event": "UserPromptSubmit",
                      "outcome": "Installed"
                    },
                    {
                      "event": "Stop",
                      "outcome": "Installed"
                    },
                    {
                      "event": "Notification",
                      "outcome": "Installed"
                    },
                    {
                      "event": "SessionEnd",
                      "outcome": "Installed"
                    }
                  ],
                  "allHooksDisabledIn": null
                }
                """,
                """
                {
                  "settingsPath": "<settings>",
                  "current": true,
                  "events": [
                    {
                      "event": "AgentNaming",
                      "outcome": "Installed",
                      "installedCommand": "<nitro> agent hook venti agent-naming"
                    },
                    {
                      "event": "SessionStart",
                      "outcome": "Installed",
                      "installedCommand": "<nitro> agent hook venti session-start"
                    },
                    {
                      "event": "UserPromptSubmit",
                      "outcome": "Installed",
                      "installedCommand": "<nitro> agent hook venti user-prompt-submit"
                    },
                    {
                      "event": "Stop",
                      "outcome": "Installed",
                      "installedCommand": "<nitro> agent hook venti stop"
                    },
                    {
                      "event": "Notification",
                      "outcome": "Installed",
                      "installedCommand": "<nitro> agent hook venti notification"
                    },
                    {
                      "event": "SessionEnd",
                      "outcome": "Installed",
                      "installedCommand": "<nitro> agent hook venti session-end"
                    }
                  ],
                  "allHooksDisabledIn": null
                }
                """,
                """
                {
                  "settingsPath": "<settings>",
                  "events": [
                    {
                      "event": "AgentNaming",
                      "outcome": "Removed"
                    },
                    {
                      "event": "SessionStart",
                      "outcome": "Removed"
                    },
                    {
                      "event": "UserPromptSubmit",
                      "outcome": "Removed"
                    },
                    {
                      "event": "Stop",
                      "outcome": "Removed"
                    },
                    {
                      "event": "Notification",
                      "outcome": "Removed"
                    },
                    {
                      "event": "SessionEnd",
                      "outcome": "Removed"
                    }
                  ]
                }
                """
            ]);
    }

    [Fact]
    public async Task InstallHelp_Should_DescribeTheVentiSettingsFiles_When_HelpIsRequested()
    {
        // arrange
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "venti", "install", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Add or update this CLI's Venti turn-boundary hook entries.

            Usage:
              nitro agent hooks venti install [options]

            Options:
              --scope <project|user>  Where the Venti settings file lives: 'user' (~/.venti/settings.json) or 'project' (./.venti/settings.json, read when Venti is launched from the current directory) [default: user]
              --output <json>         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help          Show help and usage information

            Example:
              nitro agent hooks venti install
              nitro agent hooks venti install --scope project
            """);
    }

    private void WriteSettings(string json) => WriteFile(SettingsPath, json);

    private static void WriteFile(string path, string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    private async Task SwitchOffStopHandlerAsync()
    {
        var settings = JsonNode.Parse(await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken))!;
        settings["hooks"]!["Stop"]![0]!["hooks"]![0]!["enabled"] = false;
        WriteSettings(settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private string ReplacePaths(string text)
        => text
            .Replace(JsonEncodedText.Encode(SettingsPath).ToString(), "<settings>")
            .Replace(JsonEncodedText.Encode(UserSettingsPath).ToString(), "<user-settings>")
            .Replace(SettingsPath, "<settings>")
            .Replace(UserSettingsPath, "<user-settings>");

    private static string NormalizeJson(string json)
        => JsonNode.Parse(json)!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Replaces the machine-specific launch prefix of each Nitro hook command.
    /// </summary>
    private static string NormalizeCommands(string settingsJson)
        => NitroCommandPrefix().Replace(settingsJson, "\"$1\": \"<nitro> agent hook venti ");

    [GeneratedRegex("\"(command|installedCommand)\": \"[^\"]* agent hook venti ")]
    private static partial Regex NitroCommandPrefix();
}
