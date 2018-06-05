using System.Reflection;
using HotChocolate.Configuration;

namespace HotChocolate.Configuration
{
    internal interface ISchemaContext
    {
        ITypeRegistry Types { get; }
        IResolverRegistry Resolvers { get; }
    }
}
