using System.Text.Json;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    /// <summary>
    /// Common extension of <see cref="OperationMessage"/>
    /// </summary>
    public static class OperationMessageExtensions
    {
        /// <summary>
        /// Parses the json document out the payload of a <see cref="DataDocumentOperationMessage"/>
        /// </summary>
        /// <param name="message">The message to parse</param>
        /// <returns>The parses payload as a <see cref="JsonDocument"/></returns>
        public static JsonDocument ParseDocument(this DataDocumentOperationMessage message)
        {
            var reader = new Utf8JsonReader(message.Payload.Span);
            return JsonDocument.ParseValue(ref reader);
        }
    }
}
