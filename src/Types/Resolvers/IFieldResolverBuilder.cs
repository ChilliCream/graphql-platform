using System.Collections.Generic;
using System.Threading;

namespace HotChocolate.Resolvers
{
    public interface IFieldResolverBuilder
    {
        IEnumerable<FieldResolver> Build(
            IEnumerable<FieldResolverDescriptor> fieldResolverDescriptors);
    }
}