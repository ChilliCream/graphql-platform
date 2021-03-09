using System;

namespace StrawberryShake
{
    /// <summary>
    /// A snapshot of the current stored operation.
    /// </summary>
    public readonly struct StoredOperationVersion
    {
        /// <summary>
        /// Creates a new instance of <see cref="StoredOperationVersion"/>.
        /// </summary>
        /// <param name="request">
        /// The operation request.
        /// </param>
        /// <param name="result">
        /// The last result.
        /// </param>
        /// <param name="version">
        /// The current entity store version of this operation.
        /// </param>
        /// <param name="subscribers">
        /// The count of subscribers that are listening to this operation.
        /// </param>
        /// <param name="lastModified">
        /// The time when this operation was last modified.
        /// </param>
        public StoredOperationVersion(
            OperationRequest request,
            IOperationResult? result,
            ulong version,
            int subscribers,
            DateTime lastModified)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Result = result;
            Subscribers = subscribers;
            LastModified = lastModified;
            Version = version;
        }

        /// <summary>
        /// Gets the operation request.
        /// </summary>
        public OperationRequest Request { get; }

        /// <summary>
        /// Gets the last result.
        /// </summary>
        public IOperationResult? Result { get; }

        /// <summary>
        /// Gets the current entity store version of this operation.
        /// </summary>
        public ulong Version { get; }

        /// <summary>
        /// Gets the count of subscribers that are listening to this operation.
        /// </summary>
        public int Subscribers { get; }

        /// <summary>
        /// Gets the time when this operation was last modified.
        /// </summary>
        public DateTime LastModified { get; }
    }
}
