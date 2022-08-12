using StrawberryShake.Serialization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Common extensions for <see cref="ISerializer"/>
/// </summary>
public static class SerializerServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ISerializer"/> for Strawberry Shake.
    /// </summary>
    /// <param name="services">The service collection to register the serializer</param>
    /// <param name="serializer">The instance of the serializer</param>
    /// <typeparam name="T">The type of the serializer</typeparam>
    /// <returns>The service collection form <paramref name="services"/></returns>
    public static IServiceCollection AddSerializer<T>(
        this IServiceCollection services,
        T serializer) where T : ISerializer
        => services.AddSingleton<ISerializer>(serializer);

    /// <summary>
    /// Registers a <see cref="ISerializer"/> for Strawberry Shake
    /// </summary>
    /// <param name="services">The service collection to register the serializer</param>
    /// <typeparam name="T">The type of the serializer</typeparam>
    /// <returns>The service collection form <paramref name="services"/></returns>
    public static IServiceCollection AddSerializer<T>(this IServiceCollection services)
        where T : class, ISerializer
        => services.AddSingleton<ISerializer, T>();
}
