using System.Text.Json;
using System.Threading.Tasks;

namespace StrawberryShake.Tools
{
    public interface IConfigurationStore
    {
        Configuration New();

        Task<Configuration?> TryLoadAsync(string path);

        Task SaveAsync(string path, Configuration configuration);

        bool Exists(string path);
    }

    public class DefaultConfigurationStore
        : IConfigurationStore
    {
        private readonly IFileSystem _fileSystem;

        public DefaultConfigurationStore(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public Configuration New() => new Configuration();

        public bool Exists(string path)
        {
            return _fileSystem.FileExists(
                _fileSystem.CombinePath(path, WellKnownFiles.Config));
        }

        public async Task<Configuration?> TryLoadAsync(string path)
        {
            if (Exists(path))
            {
                try
                {
                    Configuration config;
                    string configFile = _fileSystem.CombinePath(path, WellKnownFiles.Config);
                    byte[] buffer = await _fileSystem.ReadAllBytesAsync(configFile);

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
                    });
            });
        }
    }
}
