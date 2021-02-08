namespace HotChocolate.Configuration
{
    /// <summary>
    /// This interceptor allows to hook into type registry events.
    /// </summary>
    public interface ITypeRegistryInterceptor
    {
        /// <summary>
        /// This event is called after the type was registered with the type registry.
        /// </summary>
        /// <param name="context"></param>
        void OnTypeRegistered(ITypeDiscoveryContext context);
    }
}
