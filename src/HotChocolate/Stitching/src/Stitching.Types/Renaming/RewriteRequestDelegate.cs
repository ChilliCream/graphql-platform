using System.Threading.Tasks;

namespace HotChocolate.Stitching.Types.Renaming;

/// <summary>
/// A function that can process a GraphQL rewrite request.
/// </summary>
/// <param name="context">The <see cref="IRewriteContext"/> for the request.</param>
/// <returns>A task that represents the completion of request processing.</returns>
public delegate ValueTask RewriteRequestDelegate(IRewriteContext context);
