using System.CommandLine.Invocation;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Commands.Init.Mcp.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Commands.Init.Mcp;

internal sealed class InitMcpCommand : Command
{
    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true
    };

    public InitMcpCommand() : base("mcp")
    {
        Description = "Configure the Nitro MCP server for your AI agent";

        AddOption(Opt<AgentOption>.Instance);
        AddOption(Opt<WorkingDirectoryOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        console.WriteLine();
        console.WriteLine("Configuring Nitro MCP server");
        console.WriteLine();

        var workingDirectory = context.ParseResult
            .GetValueForOption(Opt<WorkingDirectoryOption>.Instance)!;

        // Step 1: agent selection
        var agentName = context.ParseResult.GetValueForOption(Opt<AgentOption>.Instance);
        if (agentName is null)
        {
            agentName = console.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which AI agent do you want to configure?".AsQuestion())
                    .AddChoices(
                        AgentNames.ClaudeCode,
                        AgentNames.Cursor,
                        AgentNames.VSCode,
                        AgentNames.Windsurf,
                        AgentNames.Other));
        }

        if (agentName == AgentNames.Other)
        {
            PrintManualInstructions(console);
            return ExitCodes.Success;
        }

        // Step 2: resolve target file
        var configPath = ResolveConfigPath(workingDirectory, agentName);
        console.OkQuestion("Config file", configPath);

        // Step 3: merge / write
        await WriteAgentConfigAsync(console, agentName, configPath, cancellationToken);

        // Step 4: optional settings bootstrap
        var settingsPath = Path.Combine(workingDirectory, ".nitro", "settings.json");
        if (!File.Exists(settingsPath))
        {
            var createSettings = await console.ConfirmAsync(
                "Create .nitro/settings.json to store project settings?",
                cancellationToken);

            if (createSettings)
            {
                await BootstrapSettingsAsync(settingsPath, cancellationToken);
                console.OkLine($"Created {".nitro/settings.json".AsHighlight()}");
            }
        }

        console.WriteLine();
        console.OkLine($"Nitro MCP server configured for {agentName.AsHighlight()}.");
        console.MarkupLine(
            "  [dim]Restart your agent to pick up the new configuration.[/]");

        return ExitCodes.Success;
    }

    private static string ResolveConfigPath(string workingDirectory, string agentName)
        => agentName switch
        {
            AgentNames.ClaudeCode => Path.Combine(workingDirectory, ".mcp.json"),
            AgentNames.Cursor => Path.Combine(workingDirectory, ".cursor", "mcp.json"),
            AgentNames.VSCode => Path.Combine(workingDirectory, ".vscode", "settings.json"),
            AgentNames.Windsurf => Path.Combine(workingDirectory, ".windsurf", "mcp.json"),
            _ => throw new ArgumentOutOfRangeException(nameof(agentName))
        };

    private static Task WriteAgentConfigAsync(
        IAnsiConsole console,
        string agentName,
        string configPath,
        CancellationToken cancellationToken)
        => agentName == AgentNames.VSCode
            ? WriteVSCodeSettingsAsync(console, configPath, cancellationToken)
            : WriteMcpServersStyleAsync(console, configPath, cancellationToken);

    private static async Task WriteMcpServersStyleAsync(
        IAnsiConsole console,
        string configPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

        JsonObject root;

        if (File.Exists(configPath))
        {
            var existing = await File.ReadAllTextAsync(configPath, cancellationToken);
            root = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();

            var servers = root["mcpServers"]?.AsObject();
            if (servers?["nitro"] is not null)
            {
                var overwrite = await console.ConfirmAsync(
                    "A Nitro MCP entry already exists. Overwrite?",
                    cancellationToken);

                if (!overwrite)
                {
                    console.OkLine("No changes made.");
                    return;
                }
            }

            root["mcpServers"] ??= new JsonObject();
        }
        else
        {
            root = new JsonObject
            {
                ["mcpServers"] = new JsonObject()
            };
        }

        root["mcpServers"]!.AsObject()["nitro"] = BuildNitroServerNode();

        var json = root.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(configPath, json, cancellationToken);
        console.OkLine($"Written {configPath.AsHighlight()}");
    }

    private static async Task WriteVSCodeSettingsAsync(
        IAnsiConsole console,
        string configPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

        JsonObject root;

        if (File.Exists(configPath))
        {
            var existing = await File.ReadAllTextAsync(configPath, cancellationToken);
            root = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();

            var servers = root["mcp"]?["servers"]?.AsObject();
            if (servers?["nitro"] is not null)
            {
                var overwrite = await console.ConfirmAsync(
                    "A Nitro MCP entry already exists in .vscode/settings.json. Overwrite?",
                    cancellationToken);

                if (!overwrite)
                {
                    console.OkLine("No changes made.");
                    return;
                }
            }

            if (root["mcp"] is null)
            {
                root["mcp"] = new JsonObject { ["servers"] = new JsonObject() };
            }
            else
            {
                root["mcp"]!.AsObject()["servers"] ??= new JsonObject();
            }
        }
        else
        {
            root = new JsonObject
            {
                ["mcp"] = new JsonObject
                {
                    ["servers"] = new JsonObject()
                }
            };
        }

        root["mcp"]!["servers"]!.AsObject()["nitro"] = BuildNitroServerNode();

        var json = root.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(configPath, json, cancellationToken);
        console.OkLine($"Written {".vscode/settings.json".AsHighlight()}");
    }

    private static JsonObject BuildNitroServerNode()
        => new()
        {
            ["command"] = "npx",
            ["args"] = new JsonArray("@chillicream/nitro", "mcp", "serve")
        };

    private static async Task BootstrapSettingsAsync(
        string settingsPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        var content = new JsonObject
        {
            ["$schema"] = "https://chillicream.com/schemas/nitro/settings/v1.json"
        };
        var json = content.ToJsonString(s_writeOptions);
        await File.WriteAllTextAsync(settingsPath, json, cancellationToken);
    }

    private static void PrintManualInstructions(IAnsiConsole console)
    {
        console.WriteLine();
        console.MarkupLine(
            "To configure Nitro MCP manually, add the following to your agent's MCP config file:");
        console.WriteLine();
        console.MarkupLine(
            """
              [bold]"nitro"[/]: {
                [bold]"command"[/]: "npx",
                [bold]"args"[/]: ["@chillicream/nitro", "mcp", "serve"]
              }
            """);
    }
}
