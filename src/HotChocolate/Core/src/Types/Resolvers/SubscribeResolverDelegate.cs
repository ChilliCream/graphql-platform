using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// This delegates describes the subscribe resolver interface that the execution engine 
    /// uses to subscribe to a event stream.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <returns>
    /// Returns the the event stream.
    /// </returns>
    public delegate ValueTask<IAsyncEnumerable<object>> SubscribeResolverDelegate(
        IResolverContext context);
}
