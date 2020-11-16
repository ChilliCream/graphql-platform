using System;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionConventionDescriptor
    {
        /// <summary>
        /// Specifies the projection provider.
        /// </summary>
        /// <typeparam name="TProvider">The projection provider type.</typeparam>
        IProjectionConventionDescriptor Provider<TProvider>()
            where TProvider : class, IProjectionProvider;

        /// <summary>
        /// Specifies the projection provider.
        /// </summary>
        /// <param name="provider">The concrete projection provider that shall be used.</param>
        /// <typeparam name="TProvider">The projection provider type.</typeparam>
        IProjectionConventionDescriptor Provider<TProvider>(TProvider provider)
            where TProvider : class, IProjectionProvider;

        /// <summary>
        /// Specifies the projection provider.
        /// </summary>
        /// <param name="provider">The projection provider type.</param>
        IProjectionConventionDescriptor Provider(Type provider);

        /// <summary>
        /// Add a extensions that is applied to <see cref="ProjectionProvider"/>
        /// </summary>
        /// <typeparam name="TExtension">The filter provider extension type.</typeparam>
        IProjectionConventionDescriptor AddProviderExtension<TExtension>()
            where TExtension : class, IProjectionProviderExtension;

        /// <summary>
        /// Add a extensions that is applied to <see cref="ProjectionProvider"/>
        /// </summary>
        /// <param name="provider">
        /// The concrete filter provider extension that shall be used.
        /// </param>
        /// <typeparam name="TExtension">The filter provider extension type.</typeparam>
        IProjectionConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
            where TExtension : class, IProjectionProviderExtension;
    }
}
