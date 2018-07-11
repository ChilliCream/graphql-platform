using System.Reflection;
using HotChocolate.Configuration;

namespace HotChocolate.Configuration
{
    internal interface ISchemaContext
    {
        ITypeRegistry Types { get; }
        IDirectiveRegistry Directives { get; }
        IResolverRegistry Resolvers { get; }
    }
}
