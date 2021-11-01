using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Helpers;

partial class Build : NukeBuild
{
    private readonly string _shippedApiFile = "PublicAPI.Shipped.txt";
    private readonly string _unshippedApiFile = "PublicAPI.Unshipped.txt";
    private readonly string _removedApiPrefix = "*REMOVED*";

    [Parameter] readonly string From;
    [Parameter] readonly string To;
    [Parameter] readonly bool Breaking;

    Target CheckPublicApi => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Helpers.TryDelete(PublicApiSolutionFile);

            DotNetBuildSonarSolution(
                PublicApiSolutionFile,
                include: file =>
                    !Path.GetFileNameWithoutExtension(file)
                        .EndsWith("tests", StringComparison.OrdinalIgnoreCase));

            DotNetBuild(c => c
                .SetProjectFile(PublicApiSolutionFile)
                .SetNoRestore(InvokedTargets.Contains(Restore))
                .SetConfiguration(Configuration)
                .SetProperty("RequireDocumentationOfPublicApiChanges", true));
        });

    Target AddUnshippedApi => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            TryDelete(PublicApiSolutionFile);

            DotNetBuildSonarSolution(
                PublicApiSolutionFile,
                include: file =>
                    !Path.GetFileNameWithoutExtension(file)
                        .EndsWith("tests", StringComparison.OrdinalIgnoreCase));

            // last we run the actual dotnet format command.
            DotNet($@"format ""{PublicApiSolutionFile}"" analyzers", workingDirectory: RootDirectory);
        });

    Target DiffShippedApi => _ => _
        .Executes(() =>
        {
            var from = string.IsNullOrEmpty(From) ? GitRepository.Branch : From;
            var to = string.IsNullOrEmpty(To) ? "main" : To;

            if (from == to)
            {
                Colorful.Console.WriteLine("Nothing to diff here.", Color.Yellow);
                return;
            }

            AbsolutePath shippedPath = SourceDirectory / "**" / _shippedApiFile;

            Git($@" --no-pager diff --minimal -U0 --word-diff ""{from}"" ""{to}"" -- ""{shippedPath}""", RootDirectory);
        });

    Target DisplayUnshippedApi => _ => _
        .Executes(async () =>
        {
            var unshippedFiles = Directory.GetFiles(SourceDirectory, _unshippedApiFile, SearchOption.AllDirectories);

            if (Breaking)
            {
                Colorful.Console.WriteLine("Unshipped breaking changes:", Color.Red);
            }
            else
            {
                Colorful.Console.WriteLine("Unshipped changes:");
            }

            Colorful.Console.WriteLine();

            foreach (var unshippedFile in unshippedFiles)
            {
                IEnumerable<string> unshippedApis = await GetNonEmptyLinesAsync(unshippedFile);

                if (Breaking)
                {
                    unshippedApis = unshippedApis.Where(u => u.StartsWith(_removedApiPrefix)).ToList();
                }

                if (!unshippedApis.Any())
                {
                    continue;
                }

                foreach (var unshippedApi in unshippedApis)
                {
                    if (unshippedApi.StartsWith(_removedApiPrefix))
                    {
                        var value = unshippedApi[_removedApiPrefix.Length..];
                        Colorful.Console.WriteLine(value);
                    }
                    else
                    {
                        Colorful.Console.WriteLine(unshippedApi);
                    }
                }
            }
        });

    Target MarkApiShipped => _ => _
        .Executes(async () =>
        {
            Colorful.Console.WriteLine("This is only supposed to be executed after a release has been published.", Color.Yellow);
            Colorful.Console.WriteLine("If you just want to stage your API changes, use the AddUnshippedApi script.", Color.Yellow);
            Colorful.Console.WriteLine("Continue? (y/n)", Color.Yellow);

            if (Colorful.Console.ReadKey().Key != ConsoleKey.Y)
            {
                return;
            }

            Colorful.Console.WriteLine();

            var shippedFiles = Directory.GetFiles(SourceDirectory, _shippedApiFile, SearchOption.AllDirectories);

            foreach (var shippedFile in shippedFiles)
            {
                var projectDir = Path.GetDirectoryName(shippedFile);
                var unshippedFile = Path.Join(projectDir, _unshippedApiFile);

                if (!File.Exists(unshippedFile))
                {
                    continue;
                }

                List<string> unshippedApis = await GetNonEmptyLinesAsync(unshippedFile);

                if (!unshippedApis.Any())
                {
                    continue;
                }

                List<string> shippedApis = await GetNonEmptyLinesAsync(shippedFile);

                List<string> removedApis = new();

                foreach (var unshippedApi in unshippedApis)
                {
                    if (unshippedApi.StartsWith(_removedApiPrefix))
                    {
                        var value = unshippedApi[_removedApiPrefix.Length..];
                        removedApis.Add(value);
                    }
                    else
                    {
                        shippedApis.Add(unshippedApi);
                    }
                }

                IOrderedEnumerable<string> newShippedApis = shippedApis
                    .Where(s => !removedApis.Contains(s))
                    .Distinct()
                    .OrderBy(s => s);

                await File.WriteAllLinesAsync(shippedFile, newShippedApis, Encoding.ASCII);
                await File.WriteAllTextAsync(unshippedFile, "", Encoding.ASCII);
            }
        });

    private static async Task<List<string>> GetNonEmptyLinesAsync(string filepath)
    {
        var lines = await File.ReadAllLinesAsync(filepath);

        return lines.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }
}
