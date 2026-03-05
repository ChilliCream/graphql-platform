using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.ProjectSettings;

internal sealed class ProjectSettingsService : IProjectSettingsService
{
    private const string NitroDirectory = ".nitro";
    private const string SettingsFileName = "settings.json";

    public string? FindSettingsDirectory(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName, NitroDirectory, SettingsFileName);

            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    public async Task<ProjectSettings?> LoadAsync(
        string startDirectory,
        CancellationToken ct)
    {
        var settingsDir = FindSettingsDirectory(startDirectory);

        if (settingsDir is null)
        {
            return null;
        }

        var filePath = Path.Combine(settingsDir, NitroDirectory, SettingsFileName);

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync(
                stream,
                ProjectSettingsJsonContext.Default.ProjectSettings,
                ct);
        }
        catch
        {
            // Silently skip invalid settings files; don't block CLI usage
            return null;
        }
    }

    public ProjectContext ResolveContext(
        ProjectSettings settings,
        string settingsRoot,
        string cwd)
    {
        var activeApi = FindBestMatch(
            settings.Apis, settingsRoot, cwd, e => e.Path);
        var activeClient = FindBestMatch(
            settings.Clients, settingsRoot, cwd, e => e.Path);
        var activeMcp = FindBestMatch(
            settings.McpCollections, settingsRoot, cwd, e => e.Path);
        var activeOpenApi = FindBestMatch(
            settings.OpenApiCollections, settingsRoot, cwd, e => e.Path);

        return new ProjectContext
        {
            WorkspaceId = settings.WorkspaceId,
            CloudUrl = settings.CloudUrl,
            DefaultStage = settings.DefaultStage,
            StyleTags = settings.StyleTags ?? [],
            ActiveApi = activeApi,
            ActiveClient = activeClient,
            ActiveMcpCollection = activeMcp,
            ActiveOpenApiCollection = activeOpenApi,
            SettingsRoot = settingsRoot
        };
    }

    public async Task SaveAsync(
        ProjectSettings settings,
        string directory,
        CancellationToken ct)
    {
        var nitroDir = Path.Combine(directory, NitroDirectory);
        Directory.CreateDirectory(nitroDir);

        var filePath = Path.Combine(nitroDir, SettingsFileName);

        await using var stream = File.Open(
            filePath, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(
            stream,
            settings,
            ProjectSettingsJsonContext.Default.ProjectSettings,
            ct);
    }

    private static T? FindBestMatch<T>(
        List<T>? entries,
        string settingsRoot,
        string cwd,
        Func<T, string?> pathSelector) where T : class
    {
        if (entries is null || entries.Count == 0)
        {
            return null;
        }

        if (entries.Count == 1)
        {
            return entries[0];
        }

        // For multiple entries, find the one whose resolved path is an ancestor
        // of cwd with the longest matching prefix (most specific match)
        T? bestMatch = null;
        var bestLength = -1;

        foreach (var entry in entries)
        {
            var relPath = pathSelector(entry);

            if (relPath is null)
            {
                continue;
            }

            var absPath = Path.GetFullPath(Path.Combine(settingsRoot, relPath));

            if (cwd.StartsWith(absPath, StringComparison.OrdinalIgnoreCase)
                && absPath.Length > bestLength)
            {
                bestMatch = entry;
                bestLength = absPath.Length;
            }
        }

        // Fallback: return first entry if no path-based match
        return bestMatch ?? entries[0];
    }
}
