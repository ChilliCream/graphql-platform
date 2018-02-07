using System;

namespace Zeus.Resolvers
{
    public interface IResolverCollection
    {
        bool TryGetResolver(IServiceProvider serviceProvider,
            string typeName, string fieldName, out IResolver resolver);
        bool TryGetResolver(IServiceProvider serviceProvider,
            Type type, string fieldName, out IResolver resolver);
    }
}
