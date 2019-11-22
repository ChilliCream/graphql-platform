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
}
