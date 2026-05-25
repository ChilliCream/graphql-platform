namespace Mocha;

internal static class MessagingOperationTypeExtensions
{
    public static string ToTypeString(this MessagingOperationType type)
        => type switch
        {
            MessagingOperationType.Send => "send",
            MessagingOperationType.Receive => "receive",
            MessagingOperationType.Process => "process",
            MessagingOperationType.Settle => "settle",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
