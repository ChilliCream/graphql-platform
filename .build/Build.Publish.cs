using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Helpers;
using static Nuke.Common.Tools.NuGet.NuGetTasks;


partial class Build : NukeBuild
{
    // IEnumerable<string> ChangelogSectionNotes => ExtractChangelogSectionNotes(ChangelogFile);
    [Parameter("NuGet Source for Packages")] readonly string NuGetSource = "https://api.nuget.org/v3/index.json";
    [Parameter("NuGet Api Key")] readonly string NuGetApiKey;

    Target Pack => _ => _
        .DependsOn(Restore)
        .Produces(PackageDirectory / "*.nupkg")
        .Produces(PackageDirectory / "*.snupkg")
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            if (!InvokedTargets.Contains(Restore))
            {
                DotNetBuildSonarSolution(AllSolutionFile);
            }

            DotNetPack(c => c
                .SetProject(AllSolutionFile)
                .SetNoBuild(InvokedTargets.Contains(Compile))
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackageDirectory)
                .SetVersion(GitVersion.SemVer));

            var projFile = File.ReadAllText(StarWarsProj);
            File.WriteAllText(StarWarsProj, projFile.Replace("11.0.0-dev.14", GitVersion.SemVer));

            projFile = File.ReadAllText(EmptyServerProj);
            File.WriteAllText(EmptyServerProj, projFile.Replace("11.0.0-dev.14", GitVersion.SemVer));

            NuGetPack(c => c
                .SetVersion(GitVersion.SemVer)
                .SetOutputDirectory(PackageDirectory)
                .SetConfiguration(Configuration)
                .CombineWith(
                    t => t.SetTargetPath(StarWarsTemplateNuSpec),
                    t => t.SetTargetPath(EmptyServerTemplateNuSpec)));


            /*
            NuGetPack(c => c
                .SetVersion(GitVersion.SemVer)
                .SetOutputDirectory(PackageDirectory)
                .SetConfiguration(Configuration)
                .CombineWith(
                    t => t.SetTargetPath(StrawberryShakeNuSpec),
                    t => t.SetTargetPath(StarWarsTemplateNuSpec),
                    t => t.SetTargetPath(EmptyServerTemplateNuSpec)));
                    */

            //.SetPackageReleaseNotes(GetNuGetReleaseNotes(ChangelogFile, GitRepository)));
        });

    Target Publish => _ => _
        .DependsOn(Clean, Test, Pack)
        .Consumes(Pack)
        .Requires(() => NuGetSource)
        .Requires(() => NuGetApiKey)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            IReadOnlyCollection<AbsolutePath> packages = PackageDirectory.GlobFiles("*.nupkg");

            DotNetNuGetPush(_ => _
                    .SetSource(NuGetSource)
                    .SetApiKey(NuGetApiKey)
                    .CombineWith(packages, (_, v) => _
                        .SetTargetPath(v)),
                degreeOfParallelism: 2,
                completeOnFailure: true);
        });


    Target CleanVersions => _ => _
        .Executes(async () =>
        {
            var requestInfo = new RequestInfo();

            ISet<string> completed = File.Exists(RootDirectory / "completed.txt")
                ? File.ReadLines(RootDirectory / "completed.txt").Select(t => t.Trim()).Distinct()
                    .OrderBy(t => t).ToHashSet()
                : new HashSet<string>();

            ISet<string> completedVersions = File.Exists(RootDirectory / "completed_versions.txt")
                ? File.ReadLines(RootDirectory / "completed_versions.txt")
                    .Select(t => t.Trim()).Distinct().OrderBy(t => t).ToHashSet()
                : new HashSet<string>();

            var httpClient = new HttpClient();

            var complete = true;

            foreach (var packagedId in File.ReadLines(RootDirectory / "packages.txt")
                .Select(t => t.Trim()).Distinct().OrderBy(t => t))
            {
                if (!completed.Contains(packagedId))
                {
                    string[] versions = await TryGetVersions(httpClient, packagedId);

                    if (!await TryRemovePreviewVersions(httpClient, packagedId, versions,
                        requestInfo, completedVersions))
                    {
                        complete = false;
                        break;
                    }

                    completed.Add(packagedId);
                }
            }

            File.WriteAllText(
                RootDirectory / "completed.txt",
                string.Join(Environment.NewLine, completed.OrderBy(t => t)));

            File.WriteAllText(
                RootDirectory / "completed_versions.txt",
                string.Join(Environment.NewLine, completedVersions.OrderBy(t => t)));

            Logger.Success(complete
                ? "All packages cleaned up."
                : $"Batch Completed, next run at: {DateTime.Now.AddHours(1)}");
        });

    async Task<string[]> TryGetVersions(HttpClient httpClient, string packagedId)
    {
        HttpResponseMessage responseMessage =
            await httpClient.GetAsync(
                $"https://api.nuget.org/v3-flatcontainer/{packagedId.ToLower()}/index.json");

        if (responseMessage.IsSuccessStatusCode)
        {
            VersionInfo versionInfo =
                JsonConvert.DeserializeObject<VersionInfo>(
                    await responseMessage.Content.ReadAsStringAsync());

            return versionInfo.Versions.Where(s => s.Contains("-")).ToArray();
        }

        return null;
    }

    async Task<bool> TryRemovePreviewVersions(
        HttpClient httpClient,
        string packagedId,
        string[] versions,
        RequestInfo requestInfo,
        ISet<string> completedVersions)
    {
        var completed = true;

        foreach (var version in versions)
        {
            var key = $"{packagedId}.{version}";
            if (!completedVersions.Contains(key))
            {
                if (requestInfo.Add())
                {
                    var apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
                    using var response = new HttpRequestMessage(
                        HttpMethod.Delete,
                        $"https://www.nuget.org/api/v2/package/{packagedId}/{version}");
                    response.Headers.Add("X-NuGet-ApiKey", apiKey);

                    using HttpResponseMessage responseMessage = await httpClient.SendAsync(response);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        Logger.Success($"{requestInfo.Count} Unlisted {key}");
                        completedVersions.Add(key);
                    }
                    else
                    {
                        Logger.Warn($"Request Failed: {responseMessage.StatusCode}");
                        completed = false;
                        break;
                    }
                }
                else
                {
                    Logger.Warn($"Limit used up.");
                    completed = false;
                    break;
                }
            }
        }

        await File.WriteAllTextAsync(
            RootDirectory / "completed_versions.txt",
            string.Join(Environment.NewLine, completedVersions));

        return completed;
    }

    class VersionInfo
    {
        public string[] Versions { get; set; }
    }

    class RequestInfo
    {
        public int Count { get; set; }
        public int Limit { get; set; } = 250;

        public bool Add()
        {
            if (Count < Limit)
            {
                Count++;
                return true;
            }

            return false;
        }
    }
}
