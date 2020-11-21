using System.Threading.Tasks;
using StrawberryShake.Tools.Options;

namespace StrawberryShake.Tools.Abstractions
{
    public interface ICommandHandler<in T> where T : BaseOptions
    {
        ValueTask<int> ExecuteAsync(T options);
    }
}
