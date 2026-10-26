namespace Mocha.Middlewares;

/// <summary>
/// Persists a dispatch envelope without sending it to the transport.
/// </summary>
internal interface IDispatchMessageStore
{
    ValueTask PersistAsync(IDispatchContext context);
}
