using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public interface IUIOptionsAccessor
    {
        /// <summary>
        /// The path of the UI middleware.
        /// </summary>
        PathString Path { get; }

        /// <summary>
        /// The path of the query middleware.
        /// This is basically where the UI component
        /// send its requests to execute its queries.
        /// </summary>
        PathString QueryPath { get; }
    }
}
