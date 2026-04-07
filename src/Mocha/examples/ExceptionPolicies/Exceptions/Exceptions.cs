namespace ExceptionPolicies.Exceptions;

/// <summary>
/// Transient database failure  worth retrying because it usually resolves quickly.
/// </summary>
public class TransientDatabaseException(string message) : Exception(message);

/// <summary>
/// The message payload is malformed  retrying will never help.
/// </summary>
public class MessageValidationException(string message) : Exception(message);

/// <summary>
/// The message was already processed  expected in at-least-once delivery.
/// </summary>
public class DuplicateMessageException(string message) : Exception(message);

/// <summary>
/// Payment gateway returned an error  flaky but usually recovers.
/// </summary>
public class PaymentGatewayException(string message) : Exception(message);

/// <summary>
/// Auth token expired  immediate retry is pointless, need to wait for refresh.
/// </summary>
public class AuthTokenExpiredException(string message) : Exception(message);

/// <summary>
/// External service is completely unavailable  needs time to recover.
/// </summary>
public class ExternalServiceUnavailableException(string message) : Exception(message);

/// <summary>
/// HTTP-level failure with a status code for conditional policy matching.
/// </summary>
public class HttpServiceException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>
/// Corrupt or unparseable message payload  a poison message.
/// </summary>
public class PoisonMessageException(string message) : Exception(message);
