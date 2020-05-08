using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Relay
{
    public interface IConnectionResolver
    {
        Task<IConnection> ResolveAsync(
            object source, 
            int first, 
            int last, 
            string after, 
            string before, 
            CancellationToken cancellationToken);
    }
    
    public interface IConnectionResolver<T> : IConnectionResolver
    {
        Task<IConnection> ResolveAsync(
            T source, 
            int first, 
            int last, 
            string after, 
            string before, 
            CancellationToken cancellationToken);
    }
}
