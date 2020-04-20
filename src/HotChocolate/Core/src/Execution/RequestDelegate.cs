using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    /// <summary>
    /// A function that can process a GraphQL request.
    /// </summary>
    /// <param name="context">The <see cref="IRequestContext"/> for the request.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public delegate Task RequestDelegate(IRequestContext context);
}
