using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public interface IResultParser<T>
    {
        Task<T> ParseAsync(Stream stream);
    }
}
