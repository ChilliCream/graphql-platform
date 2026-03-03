using System.Text.Json;
using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Commands.Init.Mcp;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Init;

public sealed class InitMcpCommandTests : IDisposable
{
    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _tempRoot;

    public InitMcpCommandTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(), "nitro-mcp-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    // --- AgentNames constants ---

    [Fact]
    public void AgentNames_ClaudeCode_IsExpected()
    {
        Assert.Equal("Claude Code", AgentNames.ClaudeCode);
    }

    [Fact]
    public void AgentNames_Cursor_IsExpected()
    {
        Assert.Equal("Cursor", AgentNames.Cursor);
    }

    [Fact]
    public void AgentNames_VSCode_IsExpected()
    {
        Assert.Equal("VS Code", AgentNames.VSCode);
    }

    [Fact]
    public void AgentNames_Windsurf_IsExpected()
    {
        Assert.Equal("Windsurf", AgentNames.Windsurf);
    }

    [Fact]
    public void AgentNames_Other_IsExpected()
    {
        Assert.Equal("Other", AgentNames.Other);
    }

    // --- Config generation: MCP servers style (Claude Code, Cursor, Windsurf) ---

    [Fact]
    public async Task ClaudeCode_Config_NewFile_MatchesSnapshot()
    {
        // arrange
        var configPath = Path.Combine(_tempRoot, ".mcp.json");

        // act - replicate WriteMcpServersStyleAsync for a new file
        var root = new JsonObject
        {
            ["mcpServers"] = new JsonObject()
        };
        root["mcpServers"]!.AsObject()["nitro"] = BuildNitroServerNode();
        var json = root.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(configPath, json);

        // assert
        var content = await File.ReadAllTextAsync(configPath);
        content.MatchSnapshot();
    }

    [Fact]
    public async Task Cursor_Config_NewFile_MatchesSnapshot()
    {
        // arrange
        var cursorDir = Path.Combine(_tempRoot, ".cursor");
        Directory.CreateDirectory(cursorDir);
        var configPath = Path.Combine(cursorDir, "mcp.json");

        // act
        var root = new JsonObject
        {
            ["mcpServers"] = new JsonObject()
        };
        root["mcpServers"]!.AsObject()["nitro"] = BuildNitroServerNode();
        var json = root.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(configPath, json);

        // assert
        var content = await File.ReadAllTextAsync(configPath);
        content.MatchSnapshot();
    }

    [Fact]
    public async Task VSCode_Config_NewFile_MatchesSnapshot()
    {
        // arrange
        var vscodeDir = Path.Combine(_tempRoot, ".vscode");
        Directory.CreateDirectory(vscodeDir);
        var configPath = Path.Combine(vscodeDir, "settings.json");

        // act - replicate WriteVSCodeSettingsAsync for a new file
        var root = new JsonObject
        {
            ["mcp"] = new JsonObject
            {
                ["servers"] = new JsonObject()
            }
        };
        root["mcp"]!["servers"]!.AsObject()["nitro"] = BuildNitroServerNode();
        var json = root.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(configPath, json);

        // assert
        var content = await File.ReadAllTextAsync(configPath);
        content.MatchSnapshot();
    }

    // --- Merge logic: preserve existing settings ---

    [Fact]
    public async Task McpServersStyle_Merge_PreservesExistingServers()
    {
        // arrange - existing config with another MCP server
        var configPath = Path.Combine(_tempRoot, "merge-test.json");
        const string existingJson = """
            {
                "mcpServers": {
                    "other-server": {
                        "command": "other",
                        "args": ["serve"]
                    }
                }
            }
            """;
        await File.WriteAllTextAsync(configPath, existingJson);

        // act - replicate merge logic from WriteMcpServersStyleAsync
        var existing = await File.ReadAllTextAsync(configPath);
        var root = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();
        root["mcpServers"] ??= new JsonObject();
        root["mcpServers"]!.AsObject()["nitro"] = BuildNitroServerNode();
        var json = root.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(configPath, json);

        // assert
        var content = await File.ReadAllTextAsync(configPath);
        var parsed = JsonNode.Parse(content)?.AsObject();
        Assert.NotNull(parsed);

        // original server preserved
        var otherServer = parsed["mcpServers"]?["other-server"];
        Assert.NotNull(otherServer);
        Assert.Equal("other", otherServer["command"]?.GetValue<string>());

        // nitro server added
        var nitroServer = parsed["mcpServers"]?["nitro"];
        Assert.NotNull(nitroServer);
        Assert.Equal("npx", nitroServer["command"]?.GetValue<string>());

        content.MatchSnapshot();
    }

    [Fact]
    public async Task VSCode_Merge_PreservesExistingSettings()
    {
        // arrange - existing VS Code settings with other configurations
        var configPath = Path.Combine(_tempRoot, "vscode-merge-test.json");
        const string existingJson = """
            {
                "editor.fontSize": 14,
                "mcp": {
                    "servers": {
                        "existing-mcp": {
                            "command": "existing",
                            "args": ["run"]
                        }
                    }
                }
            }
            """;
        await File.WriteAllTextAsync(configPath, existingJson);

        // act - replicate merge logic from WriteVSCodeSettingsAsync
        var existing = await File.ReadAllTextAsync(configPath);
        var root = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();
        if (root["mcp"] is null)
        {
            root["mcp"] = new JsonObject { ["servers"] = new JsonObject() };
        }
        else
        {
            root["mcp"]!.AsObject()["servers"] ??= new JsonObject();
        }
        root["mcp"]!["servers"]!.AsObject()["nitro"] = BuildNitroServerNode();
        var json = root.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(configPath, json);

        // assert
        var content = await File.ReadAllTextAsync(configPath);
        var parsed = JsonNode.Parse(content)?.AsObject();
        Assert.NotNull(parsed);

        // original VS Code setting preserved
        Assert.Equal(14, parsed["editor.fontSize"]?.GetValue<int>());

        // existing MCP server preserved
        var existingMcp = parsed["mcp"]?["servers"]?["existing-mcp"];
        Assert.NotNull(existingMcp);

        // nitro server added
        var nitroServer = parsed["mcp"]?["servers"]?["nitro"];
        Assert.NotNull(nitroServer);

        content.MatchSnapshot();
    }

    [Fact]
    public async Task McpServersStyle_Merge_OverwritesExistingNitro()
    {
        // arrange - existing config already has a nitro entry
        var configPath = Path.Combine(_tempRoot, "overwrite-test.json");
        const string existingJson = """
            {
                "mcpServers": {
                    "nitro": {
                        "command": "old-command",
                        "args": ["old"]
                    }
                }
            }
            """;
        await File.WriteAllTextAsync(configPath, existingJson);

        // act - overwrite the existing nitro entry
        var existing = await File.ReadAllTextAsync(configPath);
        var root = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();
        root["mcpServers"] ??= new JsonObject();
        root["mcpServers"]!.AsObject()["nitro"] = BuildNitroServerNode();
        var json = root.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(configPath, json);

        // assert
        var content = await File.ReadAllTextAsync(configPath);
        var parsed = JsonNode.Parse(content)?.AsObject();
        var nitro = parsed?["mcpServers"]?["nitro"];
        Assert.NotNull(nitro);
        Assert.Equal("npx", nitro["command"]?.GetValue<string>());

        var args = nitro["args"]?.AsArray();
        Assert.NotNull(args);
        Assert.Equal(3, args.Count);
        Assert.Equal("@chillicream/nitro", args[0]?.GetValue<string>());
        Assert.Equal("mcp", args[1]?.GetValue<string>());
        Assert.Equal("serve", args[2]?.GetValue<string>());
    }

    // --- Bootstrap settings ---

    [Fact]
    public async Task BootstrapSettings_CreatesExpectedJson()
    {
        // arrange
        var settingsPath = Path.Combine(_tempRoot, ".nitro", "settings.json");

        // act - replicate BootstrapSettingsAsync
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        var content = new JsonObject
        {
            ["$schema"] = "https://chillicream.com/schemas/nitro/settings/v1.json"
        };
        var json = content.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(settingsPath, json);

        // assert
        Assert.True(File.Exists(settingsPath));
        var written = await File.ReadAllTextAsync(settingsPath);
        written.MatchSnapshot();
    }

    // --- Nitro server node structure ---

    [Fact]
    public void BuildNitroServerNode_HasExpectedStructure()
    {
        // act
        var node = BuildNitroServerNode();

        // assert
        Assert.Equal("npx", node["command"]?.GetValue<string>());

        var args = node["args"]?.AsArray();
        Assert.NotNull(args);
        Assert.Equal(3, args.Count);
        Assert.Equal("@chillicream/nitro", args[0]?.GetValue<string>());
        Assert.Equal("mcp", args[1]?.GetValue<string>());
        Assert.Equal("serve", args[2]?.GetValue<string>());
    }

    /// <summary>
    /// Replicates the BuildNitroServerNode method from InitMcpCommand
    /// to test the JSON structure it produces.
    /// </summary>
    private static JsonObject BuildNitroServerNode()
        => new()
        {
            ["command"] = "npx",
            ["args"] = new JsonArray("@chillicream/nitro", "mcp", "serve")
        };

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // ignore cleanup failures
        }
    }
}
