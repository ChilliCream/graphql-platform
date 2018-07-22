using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Runtime;

namespace HotChocolate.Configuration
{
    internal interface ISchemaContext
    {
        ITypeRegistry Types { get; }
        IDirectiveRegistry Directives { get; }
        IResolverRegistry Resolvers { get; }
        ICollection<DataLoaderDescriptor> DataLoaders { get; }
    }
}
