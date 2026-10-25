namespace Mocha;

internal static class HeaderMessageKindExtensions
{
    public static bool IsReply(this IReadOnlyHeaders headers)
    {
        return headers.GetMessageKind() == MessageKind.Reply;
    }

    public static bool IsReplyOrFault(this IReadOnlyHeaders headers)
    {
        return headers.GetMessageKind() is MessageKind.Reply or MessageKind.Fault;
    }

    public static string? GetMessageKind(this IReadOnlyHeaders headers)
    {
        return headers.Get(MessageHeaders.MessageKind);
    }

    public static void SetMessageKind(this IHeaders headers, string messageKind)
    {
        headers.Set(MessageHeaders.MessageKind, messageKind);
    }
}
