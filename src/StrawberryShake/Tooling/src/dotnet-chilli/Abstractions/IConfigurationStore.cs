using System.Threading.Tasks;

namespace StrawberryShake.Tools.Abstractions
{
    public interface IConfigurationStore
    {
        Config.Configuration New();

        Task<Config.Configuration?> TryLoadAsync(string path);

        Task SaveAsync(string path, Config.Configuration configuration);

        bool Exists(string path);
    }
}
