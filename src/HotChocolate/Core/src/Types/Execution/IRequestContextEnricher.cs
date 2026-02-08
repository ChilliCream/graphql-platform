namespace HotChocolate.Execution;

/// <summary>
/// The request context enricher allows to mutate the global state and
/// enrich it with custom state or populate the request context itself.
/// </summary>
public interface IRequestContextEnricher
{
    /// <summary>
    /// Enrich the request context.
    /// </summary>
    /// <param name="context">The request context.</param>
    void Enrich(RequestContext context);
}
