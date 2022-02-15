using System;
using static StrawberryShake.Properties.Resources;

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
        /// <param name="isPatch">
        /// Defines if this is a partial result that
        /// shall be patched onto a previous one.
        /// </param>
        public Response(TBody? body, Exception? exception, bool isPatch = false)
        {
            if (body is null && exception is null)
            {
                throw new ArgumentNullException(nameof(body), Response_BodyAndExceptionAreNull);
            }

            Body = body;
            Exception = exception;
            IsPatch = isPatch;
        }

        /// <summary>
        /// The serialized response body.
        /// </summary>
        public TBody? Body { get; }

        /// <summary>
        /// The transport exception.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Defines if this is a partial result that
        /// shall be patched onto a previous one.
        /// </summary>
        public bool IsPatch { get; }

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
