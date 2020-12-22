using System;

namespace StrawberryShake
{
    /// <summary>
    /// Represents a request result object containing the
    /// server result and/or the transport exception.
    /// </summary>
    /// <typeparam name="TBody">
    /// The response data.
    /// </typeparam>
    public sealed class Response<TBody> : IDisposable where TBody : class
    {
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="Response{TBody}"/>.
        /// </summary>
        /// <param name="body">
        /// The serialized response payload.
        /// </param>
        /// <param name="exception">
        /// The transport exception.
        /// </param>
        public Response(TBody? body, Exception? exception)
        {
            if (body is null && exception is null)
            {
                throw new ArgumentNullException(nameof(body),
                    "Either body or exception have to be set.");
            }

            Body = body;
            Exception = exception;
        }

        /// <summary>
        /// The serialized response body.
        /// </summary>
        public TBody? Body { get; }

        public Exception? Exception { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (Body is IDisposable d)
                {
                    d.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
