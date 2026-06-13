namespace Mocha;

/// <summary>
/// Feature stored on the message bus to carry the ordered, IsEnabled-filtered list of bus interceptors.
/// The interceptor list is built once at MessageBusBuilder.Build() and remains immutable thereafter.
/// </summary>
internal sealed class BusInterceptorFeature
{
    /// <summary>
    /// Gets the immutable, ordered list of enabled bus interceptors for this bus instance.
    /// </summary>
    public IReadOnlyList<BusInterceptor> Interceptors { get; init; } = [];
}
