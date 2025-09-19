using System.Runtime.Serialization;
using System.Text;
using StrawberryShake.Properties;

namespace StrawberryShake.Transport.WebSockets;

internal static class ThrowHelper
{
    public static SerializationException Serialization_MessageHadNoTypeSpecified() =>
        new(Resources.Serialization_MessageHadNoTypeSpecified);

    public static SerializationException Serialization_InvalidMessageType(
        ReadOnlySpan<byte> token) =>
        new(string.Format(Resources.Serialization_InvalidMessageType,
            Encoding.UTF8.GetString(token.ToArray())));

    public static SerializationException Serialization_InvalidToken(ReadOnlySpan<byte> token) =>
        new(string.Format(Resources.Serialization_InvalidToken,
            Encoding.UTF8.GetString(token.ToArray())));

    public static SerializationException Serialization_UnknownField(ReadOnlySpan<byte> token) =>
        new(string.Format(Resources.Serialization_UnknownField,
            Encoding.UTF8.GetString(token.ToArray())));

    public static SocketOperationException Protocol_CannotStartOperationOnClosedSocket(
        string operationId) =>
        new(
            string.Format(
                Resources.Protocol_CannotStartOperationOnClosedSocket,
                operationId));

    public static SocketOperationException Protocol_CannotInitializeOnClosedSocket() =>
        new(Resources.Protocol_CannotInitializeOnClosedSocket);

    public static ArgumentException Argument_IsNullOrEmpty(string argumentName) =>
        new(string.Format(Resources.Argument_IsNullOrEmpty, argumentName), argumentName);

    public static SocketOperationException SocketClient_URIWasNotSpecified(string socketName) =>
        new(string.Format(Resources.SocketClient_URIWasNotSpecified, socketName));

    public static SocketOperationException SocketClient_ProtocolNotFound(string protocolName) =>
        new(string.Format(Resources.SocketClient_ProtocolNotFound, protocolName));

    public static SocketOperationException SessionManager_SocketWasNotInitialized(
        string socketName) =>
        new(string.Format(Resources.SessionManager_SocketWasNotInitialized, socketName));

    public static SocketOperationException SessionManager_SessionIsNotOpen() =>
        new(Resources.SessionManager_SessionIsNotOpen);

    public static SocketOperationException SessionManager_OperationWasAlreadyRegistered(
        string operationId) =>
        new(string.Format(Resources.SessionManager_OperationWasAlreadyRegistered,
            operationId));

    public static ArgumentException SocketClientPool_ClientNotFromPool(string argumentName) =>
        new(string.Format(Resources.SocketClientPool_ClientNotFromPool, argumentName),
            argumentName);
}
