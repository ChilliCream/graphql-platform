using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal delegate Task<object> ExecuteMiddleware(
        IResolverContext context,
        Func<Task<object>> executeResolver);
}
