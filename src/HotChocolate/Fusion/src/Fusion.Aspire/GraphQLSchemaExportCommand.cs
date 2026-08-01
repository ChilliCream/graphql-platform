using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

#pragma warning disable ASPIREPROCESSCOMMAND001

internal static class GraphQLSchemaExportCommand
{
    internal const string CommandName = "graphql-schema-export";
    internal const string OutputArgumentName = "output";

    public static void Register(
        IResourceBuilder<ProjectResource> builder,
        string? schemaName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (schemaName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        }

        var projectPath = IOPath.GetFullPath(
            builder.Resource.GetProjectMetadata().ProjectPath);

        builder.WithProcessCommand(
            CommandName,
            "Export GraphQL schema",
            context => CreateProcessSpec(
                projectPath,
                GetRequiredOutputPath(context),
                schemaName),
            new ProcessCommandOptions
            {
                Arguments =
                [
                    new InteractionInput
                    {
                        Name = OutputArgumentName,
                        InputType = InputType.Text,
                        Required = true
                    }
                ],
                DisplayImmediately = false,
                MaxOutputLineCount = 50,
                Visibility = ResourceCommandVisibility.None
            });
    }

    public static async Task<GraphQLSchemaExportResult> ExecuteAsync(
        IServiceProvider services,
        IResource resource,
        GraphQLSourceSchemaAnnotation declaration,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        if (declaration.Location is not SourceSchemaLocationType.CommandLineExport)
        {
            throw new InvalidOperationException(
                $"Resource '{resource.Name}' does not have a complete command-line export declaration.");
        }

        var expectedSchemaName = declaration.SourceSchemaName ?? resource.Name;

        cancellationToken.ThrowIfCancellationRequested();

        var projectPath = IOPath.GetFullPath(
            GraphQLResourceModel.GetProjectPath(resource));
        outputDirectory = IOPath.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var schemaPath = IOPath.Combine(outputDirectory, "schema.graphqls");
        var settingsPath = IOPath.Combine(outputDirectory, "schema-settings.json");

        DeleteExistingArtifact(schemaPath);
        PrepareSettingsFile(projectPath, settingsPath);

        var arguments = new InteractionInputCollection(
            [
                new InteractionInput
                {
                    Name = OutputArgumentName,
                    InputType = InputType.Text,
                    Value = schemaPath
                }
            ]);
        var commandService = services.GetRequiredService<ResourceCommandService>();
        var commandResult = await commandService.ExecuteCommandAsync(
            resource,
            CommandName,
            arguments,
            cancellationToken);

        if (commandResult.Canceled)
        {
            throw new OperationCanceledException(
                $"Schema export for resource '{resource.Name}' was canceled.",
                cancellationToken);
        }

        if (!commandResult.Success)
        {
            throw new InvalidOperationException(
                $"Schema export for resource '{resource.Name}' failed. See the resource logs for details.");
        }

        await ValidateArtifactsAsync(
            resource.Name,
            expectedSchemaName,
            schemaPath,
            settingsPath,
            cancellationToken);

        return new(schemaPath, settingsPath, projectPath);
    }

    internal static ProcessCommandSpec CreateProcessSpec(
        string projectPath,
        string schemaPath,
        string? schemaName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaPath);

        if (schemaName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        }

        projectPath = IOPath.GetFullPath(projectPath);
        schemaPath = IOPath.GetFullPath(schemaPath);

        var arguments = new List<string>
        {
            "run",
            "--project",
            projectPath,
            "--no-launch-profile",
            "--",
            "schema",
            "export",
            "--output",
            schemaPath
        };

        if (schemaName is not null)
        {
            arguments.Add("--schema-name");
            arguments.Add(schemaName);
        }

        return new ProcessCommandSpec("dotnet")
        {
            WorkingDirectory = IOPath.GetDirectoryName(projectPath),
            Arguments = arguments
        };
    }

    internal static async Task ValidateArtifactsAsync(
        string resourceName,
        string schemaName,
        string schemaPath,
        string settingsPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);

        if (!File.Exists(schemaPath) || !File.Exists(settingsPath))
        {
            throw new InvalidOperationException(
                $"Schema export for resource '{resourceName}' did not produce both "
                + "schema.graphqls and schema-settings.json.");
        }

        var schemaText = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        var settingsText = await File.ReadAllTextAsync(settingsPath, cancellationToken);

        if (string.IsNullOrWhiteSpace(settingsText))
        {
            throw new InvalidOperationException(
                $"Schema export for resource '{resourceName}' produced empty schema-settings.json.");
        }

        using var settings = JsonDocument.Parse(settingsText);
        var configuration = SchemaComposition.ReadEndpointConfiguration(
            resourceName,
            schemaName,
            settings);
        GraphQLSourceSchemaValidator.Validate(
            resourceName,
            configuration,
            schemaText);
    }

    internal static void PrepareSettingsFile(
        string projectPath,
        string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);

        var projectSettingsPath = IOPath.Combine(
            IOPath.GetDirectoryName(IOPath.GetFullPath(projectPath))!,
            "schema-settings.json");
        settingsPath = IOPath.GetFullPath(settingsPath);

        DeleteExistingArtifact(settingsPath);

        if (File.Exists(projectSettingsPath))
        {
            File.Copy(projectSettingsPath, settingsPath);
        }
    }

    private static string GetRequiredOutputPath(ExecuteCommandContext context)
        => context.Arguments.GetString(OutputArgumentName)
            ?? throw new InvalidOperationException(
                "The GraphQL schema export command requires an output path.");

    private static void DeleteExistingArtifact(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

internal sealed record GraphQLSchemaExportResult(
    string SchemaPath,
    string SettingsPath,
    string ProjectPath);

#pragma warning restore ASPIREPROCESSCOMMAND001
