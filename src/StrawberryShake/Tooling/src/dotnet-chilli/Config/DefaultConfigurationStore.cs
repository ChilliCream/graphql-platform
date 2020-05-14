using System.Text.Json;
using System.Threading.Tasks;
using StrawberryShake.Tools.Abstractions;

namespace StrawberryShake.Tools.Config
{
    public class DefaultConfigurationStore : IConfigurationStore
    {
        private readonly IFileSystem _fileSystem;

        public DefaultConfigurationStore(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public Configuration New() => new Configuration();

        public bool Exists(string path) => _fileSystem.FileExists(_fileSystem.CombinePath(path, WellKnownFiles.Config));

        public async Task<Configuration?> TryLoadAsync(string path)
        {
            if (Exists(path))
            {
                try
                {
                    Configuration config;
                    var configFile = _fileSystem.CombinePath(path, WellKnownFiles.Config);
                    var buffer = await _fileSystem.ReadAllBytesAsync(configFile).ConfigureAwait(false);

                    config = JsonSerializer.Deserialize<Configuration>(
                        buffer,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        });

                    return config;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public async Task SaveAsync(string path, Configuration configuration)
        {
            string configFile = _fileSystem.CombinePath(path, WellKnownFiles.Config);
            await _fileSystem.WriteToAsync(configFile, async stream =>
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    configuration,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    }).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }
}
