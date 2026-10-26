namespace Mocha.Middlewares;

/// <summary>
/// Persists a dispatch envelope.
/// </summary>
internal interface IDispatchMessageStore
{
    ValueTask PersistAsync(IDispatchContext context);
}
