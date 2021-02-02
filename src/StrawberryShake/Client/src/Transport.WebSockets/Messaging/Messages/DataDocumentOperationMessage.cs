using System;
using System.Buffers;

namespace StrawberryShake.Transport.WebSockets.Messages
{
    /// <summary>
    /// The <see cref="DataDocumentOperationMessage{T}"/> is used to transport a data payload to the
    /// socket operation
    /// </summary>
    public class DataDocumentOperationMessage<TData> : OperationMessage<TData>
    {
        /// <summary>
        /// Creates a new instance of a <see cref="OperationMessage"/>
        /// </summary>
        /// <param name="payload">
        /// The payload of the message
        /// </param>
        public DataDocumentOperationMessage(TData payload)
            : base(OperationMessageType.Data, payload)
        {
        }
    }
}
