#nullable enable

namespace HotChocolate.Configuration
{
    public interface ITypeInterceptor
        : ITypeInitializationInterceptor
        , ITypeScopeInterceptor
    {
    }
}
