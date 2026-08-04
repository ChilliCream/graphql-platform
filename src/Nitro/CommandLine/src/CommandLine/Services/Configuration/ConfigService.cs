using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using static System.Environment.SpecialFolder;
using static System.Environment.SpecialFolderOption;

namespace ChilliCream.Nitro.CommandLine.Services.Configuration;

internal class ConfigurationService(IFileSystem fileSystem) : IConfigurationService
{
    private static readonly string s_configFolder =
        Path.Combine(Environment.GetFolderPath(ApplicationData, Create), "nitro");

    public async Task<T?> GetAsync<T>(CancellationToken cancellationToken)
        where T : IConfigurationFile
    {
        try
        {
            await EnsureConfigFile<T>(cancellationToken);

            var config = default(T);

            var configFile = Path.Combine(s_configFolder, T.FileName);
            if (fileSystem.FileExists(configFile))
            {
                try
                {
                    await using var stream = fileSystem.OpenReadStream(configFile);

                    config = await JsonSerializer
                        .DeserializeAsync(
                            stream,
                            (JsonTypeInfo<T>)T.TypeInfo,
                            cancellationToken: cancellationToken);
                }
                catch
                {
                    // we ignore the exception and reset the config
                }
            }

            if (config is null)
            {
                await ResetAsync<T>(cancellationToken);

                if (T.Default is T @default)
                {
                    return @default;
                }
            }

            return config;
        }
        catch
        {
            await ResetAsync<T>(cancellationToken);
            throw new ExitException($"Could not read configuration file {T.FileName}");
        }
    }

    public async Task SaveAsync<T>(
        T configuration,
        CancellationToken cancellationToken)
        where T : IConfigurationFile
    {
        try
        {
            await EnsureConfigFile<T>(cancellationToken);

            var configFile = Path.Combine(s_configFolder, T.FileName);
            fileSystem.DeleteFile(configFile);
            await using var stream = fileSystem.CreateFile(configFile);

            await JsonSerializer.SerializeAsync(
                stream,
                configuration,
                (JsonTypeInfo<T>)T.TypeInfo,
                cancellationToken: cancellationToken);
        }
        catch
        {
            await ResetAsync<T>(cancellationToken);
            throw new ExitException($"Could not write configuration file {T.FileName}");
        }
    }

    public Task ResetAsync<T>(CancellationToken cancellationToken) where T : IConfigurationFile
    {
        var configFile = Path.Combine(s_configFolder, T.FileName);
        if (fileSystem.FileExists(configFile))
        {
            fileSystem.DeleteFile(configFile);
        }

        return Task.CompletedTask;
    }

    private async Task EnsureConfigFile<T>(CancellationToken cancellationToken)
        where T : IConfigurationFile
    {
        if (!fileSystem.DirectoryExists(s_configFolder))
        {
            fileSystem.CreateDirectory(s_configFolder);
        }

        var configFile = Path.Combine(s_configFolder, T.FileName);
        if (!fileSystem.FileExists(configFile) && T.Default is not null)
        {
            await using var stream = fileSystem.CreateFile(configFile);
            await JsonSerializer
                .SerializeAsync(stream, T.Default, T.TypeInfo, cancellationToken: cancellationToken);
        }
    }
}
