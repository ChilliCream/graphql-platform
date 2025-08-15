using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ChilliCream.Nitro.CommandLine;
using static System.Environment.SpecialFolder;
using static System.Environment.SpecialFolderOption;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal class ConfigurationService : IConfigurationService
{
    private static readonly string _configFolder =
        Path.Combine(Environment.GetFolderPath(ApplicationData, Create), "nitro");

    public async Task<T?> GetAsync<T>(CancellationToken cancellationToken)
        where T : IConfigurationFile
    {
        try
        {
            await EnsureConfigFile<T>(cancellationToken);

            var config = default(T);

            var configFile = Path.Combine(_configFolder, T.FileName);
            if (File.Exists(configFile))
            {
                try
                {
                    await using var stream = File.OpenRead(configFile);

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

            var configFile = Path.Combine(_configFolder, T.FileName);
            File.Delete(configFile);
            await using var stream = File.OpenWrite(configFile);

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
        var configFile = Path.Combine(_configFolder, T.FileName);
        if (File.Exists(configFile))
        {
            File.Delete(configFile);
        }

        return Task.CompletedTask;
    }

    private static async Task EnsureConfigFile<T>(CancellationToken cancellationToken)
        where T : IConfigurationFile
    {
        if (!Directory.Exists(_configFolder))
        {
            Directory.CreateDirectory(_configFolder);
        }

        var configFile = Path.Combine(_configFolder, T.FileName);
        if (!File.Exists(configFile) && T.Default is not null)
        {
            await using var stream = File.OpenWrite(configFile);
            await JsonSerializer
                .SerializeAsync(stream, T.Default, T.TypeInfo, cancellationToken: cancellationToken);
        }
    }
}
