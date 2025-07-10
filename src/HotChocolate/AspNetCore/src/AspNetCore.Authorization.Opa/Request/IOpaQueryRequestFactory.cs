using HotChocolate.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// The OPA query request factory interface.
/// </summary>
public interface IOpaQueryRequestFactory
{
    /// <summary>
    /// Creates <see cref="OpaQueryRequest"/>.
    /// </summary>
    /// <param name="context">The OPA authorization handler context.
    ///     Depending on the query execution phase the context's Resource is different
    ///     see <see cref="OpaAuthorizationHandler"/> for details.
    /// </param>
    /// <param name="directive">The OPA authorization directive. See <see cref="AuthorizeDirective"/>.</param>
    OpaQueryRequest CreateRequest(
        OpaAuthorizationHandlerContext context,
        AuthorizeDirective directive);
}
