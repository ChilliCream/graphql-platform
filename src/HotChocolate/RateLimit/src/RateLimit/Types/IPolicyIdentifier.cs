using System;
using System.Threading.Tasks;

namespace HotChocolate.RateLimit
{
    public interface IPolicyIdentifier
    {
        ValueTask<string> ResolveAsync(IServiceProvider serviceProvider);
    }
}
