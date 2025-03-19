namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// The OPA authorization handler context.
/// </summary>
public class OpaAuthorizationHandlerContext
{
    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="resource">Either IMiddlewareContext or AuthorizationContext depending on the phase of
    ///     a rule execution.
    /// </param>
    public OpaAuthorizationHandlerContext(object resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        Resource = resource;
    }

    /// <summary>
    /// The object representing instance of either IMiddlewareContext or AuthorizationContext.
    /// </summary>
    public object Resource { get; }
}
