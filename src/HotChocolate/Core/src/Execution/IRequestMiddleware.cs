using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Defines request middleware that can be added to the GraphQL request pipeline.
    /// </summary>
    public interface IRequestMiddleware
    {
        /// <summary>
        /// Request handling method.
        /// </summary>
        /// <param name="context">The <see cref="IRequestContext"/> for the request.</param>
        /// <param name="next">
        /// The delegate representing the remaining middleware in the request pipeline.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that represents the execution of this middleware.
        /// </returns>
        Task InvokeAsync(IRequestContext context, RequestDelegate next);
    }
}
