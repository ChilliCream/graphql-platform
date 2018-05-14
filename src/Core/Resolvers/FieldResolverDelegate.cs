using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers
{
    public delegate Task<object> FieldResolverDelegate(
        IResolverContext context,
        CancellationToken cancellationToken);

    // TODO: the internal underlying field resolver delegate shopuld look like the following:
    // TODO : results can be Task<object>, Func<object>, Func<Task<object>>, object
    internal delegate object FieldResolverDelegate2(
        IResolverContext context,
        CancellationToken cancellationToken);
}
