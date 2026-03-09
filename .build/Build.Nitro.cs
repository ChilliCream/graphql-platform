using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using NuGet.Versioning;
using Nuke.Common;
using Serilog;

partial class Build
{
    Target UpdateNitro => _ => _
        .Executes(async () =>
        {
            var latestVersion = await GetLatestVersion("ChilliCream.Nitro.App");
            Log.Information($"Latest Nitro Version: {latestVersion}");

            var project = Project.FromFile(HotChocolateDirectoryBuildProps, new ProjectOptions());
            project.SetProperty("NitroVersion", latestVersion);
            project.Save();
        });

    static async Task<string> GetLatestVersion(string packageName)
    {
        using var client = new HttpClient();

        var url = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json";

        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(jsonString)
                .RootElement.GetProperty("versions")
                .EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !x.Contains("insider"))
                .OrderByDescending(x => SemanticVersion.Parse(x))
                .First();
        }

        throw new Exception($"Failed to retrieve package data: {response.StatusCode}");
    }
}
