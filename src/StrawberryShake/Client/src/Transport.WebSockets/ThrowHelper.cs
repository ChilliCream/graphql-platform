using System;
using System.Runtime.Serialization;
using System.Text;
using StrawberryShake.Properties;

namespace StrawberryShake.Transport.WebSockets
{
    internal static class ThrowHelper
    {
        public static SerializationException Serialization_MessageHadNoTypeSpecified() =>
            new(Resources.Serialization_MessageHadNoTypeSpecified);

        public static SerializationException Serialization_InvalidToken(
            ReadOnlySpan<byte> token) =>
            new(string.Format(Resources.Serialization_InvalidToken,
                Encoding.UTF8.GetString(token)));

        public static SerializationException Serialization_UnknownField(
            ReadOnlySpan<byte> token) =>
            new(string.Format(Resources.Serialization_UnknownField,
                Encoding.UTF8.GetString(token)));

        public static SerializationException Protocol_CannotStartOperationOnClosedSocket(
            string operationId) =>
            new(
                string.Format(
                    Resources.Protocol_CannotStartOperationOnClosedSocket,
                    operationId));

        public static SerializationException Protocol_CannotInitializeOnClosedSocket() =>
            new(Resources.Protocol_CannotInitializeOnClosedSocket);

        public static ArgumentException Argument_IsNullOrEmpty(string argumentName) =>
            new(string.Format(Resources.Argument_IsNullOrEmpty, argumentName), argumentName);
    }
}
