using System.Collections.Generic;
using System.Threading;

namespace HotChocolate.Resolvers
{
    internal interface IFieldResolverBuilder
    {
        IEnumerable<FieldResolver> Build(
            IEnumerable<IFieldResolverDescriptor> descriptors);
    }
}
