#nullable enable

namespace HotChocolate.Configuration
{
    /// <summary>
    /// Represents the type interceptors for the type initialization.
    /// </summary>
    public interface ITypeInterceptor
        : ITypeInitializationInterceptor
        , ITypeScopeInterceptor
    {
    }
}
