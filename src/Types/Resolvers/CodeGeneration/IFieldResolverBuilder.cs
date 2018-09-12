using System.Collections.Generic;
using System.Threading;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal interface IFieldResolverBuilder
    {
        IReadOnlyCollection<FieldResolver> Build(
            IEnumerable<IFieldResolverDescriptor> descriptors);
    }
}
