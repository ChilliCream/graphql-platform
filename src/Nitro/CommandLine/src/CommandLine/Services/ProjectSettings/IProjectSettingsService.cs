namespace ChilliCream.Nitro.CommandLine.Services.ProjectSettings;

internal interface IProjectSettingsService
{
    /// <summary>
    /// Loads .nitro/settings.json by searching from <paramref name="startDirectory"/>
    /// up to the filesystem root. Returns null if no settings file is found.
    /// </summary>
    Task<ProjectSettings?> LoadAsync(string startDirectory, CancellationToken ct);

    /// <summary>
    /// Returns the directory containing .nitro/settings.json, or null if not found.
    /// Searches from <paramref name="startDirectory"/> up to the filesystem root.
    /// </summary>
    string? FindSettingsDirectory(string startDirectory);

    /// <summary>
    /// Resolves a <see cref="ProjectContext"/> from loaded settings based on
    /// the current working directory for monorepo path matching.
    /// </summary>
    ProjectContext ResolveContext(
        ProjectSettings settings,
        string settingsRoot,
        string cwd);

    /// <summary>
    /// Writes .nitro/settings.json to the specified directory.
    /// Creates the .nitro directory if it does not exist.
    /// </summary>
    Task SaveAsync(ProjectSettings settings, string directory, CancellationToken ct);
}
