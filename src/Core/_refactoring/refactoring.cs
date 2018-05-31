using System.Reflection;
using HotChocolate.Configuration;

namespace HotChocolate
{
    public interface ISchemaContextR
    {
        ITypeRegistry Types { get; }
        IResolverRegistry Resolvers { get; }
    }
}
