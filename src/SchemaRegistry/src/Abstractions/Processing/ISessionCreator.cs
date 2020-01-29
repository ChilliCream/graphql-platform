using System.Threading.Tasks;

namespace MarshmallowPie.Processing
{
    public interface ISessionCreator
    {
        Task<string> CreateSessionAsync();
    }
}
