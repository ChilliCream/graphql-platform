using System.Collections.Generic;

namespace StrawberryShake
{
    // needs to implement equality
    public interface IOperationRequest
    {
        string Id { get; }

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the query document containing this operation.
        /// </summary>
        IDocument Document { get; }

        IDictionary<string, object> Variables { get; }

        IDictionary<string, object> Extensions { get; }

        IDictionary<string, object> ContextData { get; }
    }
}
