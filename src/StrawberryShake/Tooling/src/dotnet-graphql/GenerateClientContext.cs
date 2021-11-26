using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.CodeGeneration.CSharp;
using StrawberryShake.Tools.Configuration;
using static System.IO.Path;
using static System.IO.Directory;
using static System.IO.SearchOption;
using Location = StrawberryShake.CodeGeneration.CSharp.Location;

namespace StrawberryShake.Tools;

public record GenerateClientArguments(
    CommandOption Path,
    CommandOption DefaultNamespace,
    CommandOption PersistedQueryDirectory);

public record GenerateClientContext(
    string Path,
    string? DefaultNamespace = null,
    string? PersistedQueryDirectory = null);

public static class GenerateClientCommand
{
    public static void Build(CommandLineApplication download)
    {
        download.Description = "Generate GraphQL Client for C#";

        CommandOption pathArg = download.Option(
            "-p|--path",
            "The project directory.",
            CommandOptionType.SingleValue);

        CommandOption namespaceArg = download.Option(
            "-n|--defaultNamespace",
            "The default namespace that shall be used if no namespace is configured.",
            CommandOptionType.SingleValue);

        CommandOption persistedQueryDirectoryArg = download.Option(
            "-d|--persistedQueryDirectory",
            "The directory to export persisted queries to.",
            CommandOptionType.SingleValue);

        CommandOption jsonArg = download.Option(
            "-j|--json",
            "Console output as JSON.",
            CommandOptionType.NoValue);

        download.OnExecuteAsync(cancellationToken =>
        {
            var arguments = new GenerateClientArguments(
                pathArg,
                namespaceArg,
                persistedQueryDirectoryArg);
            GenerateClientCommandHandler handler =
                CommandTools.CreateHandler<GenerateClientCommandHandler>(jsonArg);
            return handler.ExecuteAsync(arguments, cancellationToken);
        });
    }
}


public class GenerateClientCommandHandler : CommandHandler<GenerateClientArguments>
{
    public GenerateClientCommandHandler(IConsoleOutput output)
    {
        Output = output;
    }

    public IConsoleOutput Output { get; }

    public override Task<int> ExecuteAsync(
        GenerateClientArguments arguments,
        CancellationToken cancellationToken)
        => GenerateAsync(new GenerateClientContext(
            arguments.Path.Value() ?? Environment.CurrentDirectory,
            arguments.DefaultNamespace.Value(),
            arguments.PersistedQueryDirectory.Value()));

    private async Task<int> GenerateAsync(GenerateClientContext context)
    {
        var codeGenServer = Combine(
            GetDirectoryName(GetType().Assembly.Location)!,
            "..", "..", "..", "gen", "BerryCodeGen.dll");

        var projectFile = GetFiles(context.Path, "*.csproj").SingleOrDefault();
        var defaultNamespace = context.DefaultNamespace ?? GetFileNameWithoutExtension(projectFile);
        var configFileNames = GetFiles(context.Path, ".graphqlrc.json", AllDirectories);
        var documentFileNames = GetFiles(context.Path, ".graphql", AllDirectories);

        var childProcess = Process.Start(
            new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = codeGenServer,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            })!;

        var client = new CSharpGeneratorClient(
            childProcess.StandardInput.BaseStream,
            childProcess.StandardOutput.BaseStream);

        foreach (var configFileName in configFileNames)
        {
            await ExecuteAsync(
                context,
                client,
                configFileName,
                documentFileNames,
                defaultNamespace);
        }

        return 0;
    }

    private async Task ExecuteAsync(
        GenerateClientContext context,
        CSharpGeneratorClient client,
        string configFileName,
        IReadOnlyList<string> documentFileNames,
        string? defaultNamespace)
    {
        using IActivity activity = Output.WriteActivity($"Generate Client for {configFileName}");

        if (!TryLoadConfig(activity, configFileName, out GraphQLConfig? config))
        {
            return;
        }

        var root = GetDirectoryName(configFileName)!;
        var code = Combine(root, config.Extensions.StrawberryShake.OutputDirectoryName);

        ClearCodeDirectory(activity, code);

        GeneratorRequest request = new(
            configFileName,
            documentFileNames,
            config.Extensions.StrawberryShake.Namespace ?? defaultNamespace,
            context.PersistedQueryDirectory);
        GeneratorResponse response = await client.GenerateAsync(request);

        foreach (GeneratorDocument document in response.Documents)
        {
            if (document.Kind is GeneratorDocumentKind.Razor or GeneratorDocumentKind.CSharp)
            {
                var fileName = code;

                if (!string.IsNullOrEmpty(document.Path))
                {
                    fileName = Combine(code, document.Path);

                    if (!Exists(fileName))
                    {
                        CreateDirectory(fileName);
                    }
                }

                fileName = Combine(fileName, document.Name);

                await File.WriteAllTextAsync(fileName, document.SourceText, Encoding.UTF8);
                Output.WriteFileCreated(fileName);
            }
        }

        if (response.Errors.Count > 0)
        {
            foreach (GeneratorError error in response.Errors)
            {
                if (error.Location is null || error.FilePath is null)
                {
                    activity.WriteError(new Error(error.Message, error.Code));
                }
                else
                {
                    activity.WriteError(new Error(
                        error.Message,
                        error.Code,
                        locations: new []
                        {
                            new HotChocolate.Location(error.Location.Line, error.Location.Column)

                        },
                        extensions: new Dictionary<string, object?>
                        {
                            { "fileName", error.FilePath }
                        }));
                }
            }
        }
    }

    private async Task<bool> TryLoadConfigAsync(
        IActivity activity,
        string fileName,
        [NotNullWhen(true)] out GraphQLConfig? config)
    {
        try
        {
            var json = await File.ReadAllTextAsync(fileName);
            config = GraphQLConfig.FromJson(json);
            return true;
        }
        catch (Exception ex)
        {
            activity.WriteError(ex);
            config = null;
            return false;
        }
    }

    private static void ClearCodeDirectory(IActivity activity, string code)
    {
        foreach (var fileName in GetFiles(code, "*.*", AllDirectories))
        {
            try
            {
                File.Delete(fileName);
            }
            catch(Exception ex)
            {
                activity.WriteError(ex);
            }
        }
    }
}
