namespace Mocha;

/// <summary>
/// Captures a bus interceptor registration descriptor, holding either the interceptor type,
/// instance, or factory for deferred instantiation.
/// Used internally during MessageBusBuilder.Build() to track and resolve interceptors.
/// </summary>
internal sealed class BusInterceptorRegistration
{
    /// <summary>
    /// The interceptor type, if registered via AddBusInterceptor&lt;T&gt;(Type).
    /// </summary>
    public Type? InterceptorType { get; init; }

    /// <summary>
    /// The interceptor instance, if registered via AddBusInterceptor(BusInterceptor).
    /// </summary>
    public BusInterceptor? InterceptorInstance { get; init; }

    /// <summary>
    /// The interceptor factory, if registered via AddBusInterceptor(Func).
    /// </summary>
    public Func<IServiceProvider, BusInterceptor>? InterceptorFactory { get; init; }
}
