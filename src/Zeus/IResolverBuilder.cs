using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus
{
    public interface IResolverBuilder
    {
        IResolverBuilder Add(string typeName, Func<IResolverContext, object> resolver);
        IResolverBuilder Add(string typeName, Func<IResolverContext, CancellationToken, Task<object>> resolver);
        IResolverBuilder Add(string typeName, string fieldName, Func<IResolverContext, object> resolver);
        IResolverBuilder Add(string typeName, string fieldName, Func<IResolverContext, CancellationToken, Task<object>> resolver);

        IResolverCollection Build();
    }


}
