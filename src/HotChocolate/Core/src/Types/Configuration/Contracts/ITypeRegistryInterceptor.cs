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
        /// <param name="discoveryContext">
        /// The type discovery context.
        /// </param>
        void OnTypeRegistered(ITypeDiscoveryContext discoveryContext);
    }
}
