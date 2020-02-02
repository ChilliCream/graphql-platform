using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Processing.InMemory
{
    public class SessionCreator
        : ISessionCreator
    {
        public ValueTask<string> CreateSessionAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<string>(Guid.NewGuid().ToString("N"));
        }
    }
}
