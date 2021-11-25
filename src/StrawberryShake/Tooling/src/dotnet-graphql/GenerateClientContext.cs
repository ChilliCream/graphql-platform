using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.CodeGeneration.CSharp;
using static System.IO.Path;

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
    public override Task<int> ExecuteAsync(
        GenerateClientArguments arguments,
        CancellationToken cancellationToken)
        => GenerateAsync(new GenerateClientContext(
            arguments.Path.Value() ?? Environment.CurrentDirectory,
            arguments.DefaultNamespace.Value(),
            arguments.PersistedQueryDirectory.Value()));

    private async Task<int> GenerateAsync(GenerateClientContext context)
    {
        Console.WriteLine(GetType().Assembly.Location);

        var projectFile = Directory.GetFiles(context.Path, "*.csproj").SingleOrDefault();
        var defaultNamespace = context.DefaultNamespace ?? GetFileNameWithoutExtension(projectFile);

        var childProcess = Process.Start(
            new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            })!;

        var client = new CSharpGeneratorClient(
            childProcess.StandardInput.BaseStream,
            childProcess.StandardOutput.BaseStream);

        foreach (var configFiles in Directory.GetFiles(
            GetDirectoryName(projectFile)!,
            ".graphqlrc.json",
            SearchOption.AllDirectories))
        {

        }


        return 1;
    }
}
