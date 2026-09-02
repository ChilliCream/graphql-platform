using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

internal static class MailDigest
{
    public static string Render(
        string actor,
        IReadOnlyList<MailMessage> messages,
        int unreadTotal)
    {
        if (messages.Count == 0)
        {
            return MailNudgeText.Format(actor, unreadTotal);
        }

        var candidates = messages
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id, StringComparer.Ordinal)
            .Take(MailDigestPolicy.MaxMessages)
            .Select(message => MailMessageDetailResult.Create(message, actor, []))
            .Select(message => TruncateBody(message, actor))
            .ToList();

        while (candidates.Count > 0)
        {
            var digest = Render(actor, candidates, unreadTotal);

            if (Encoding.UTF8.GetByteCount(digest) <= MailDigestPolicy.MaxTotalBytes)
            {
                return digest;
            }

            candidates.RemoveAt(candidates.Count - 1);
        }

        return MailNudgeText.Format(actor, unreadTotal);
    }

    private static string Render(string actor, IReadOnlyList<MailMessageDetailResult> messages, int unreadTotal)
        => $"You have {unreadTotal} unread nitro message{(unreadTotal == 1 ? "" : "s")}; {messages.Count} shown below as "
            + "`nitro agent mail read --thread --output json` prints them. "
            + $"Reply with `nitro agent mail reply --message <id> --actor {actor} --body \"...\"` "
            + $"or ack with `nitro agent mail ack --message <id> --actor {actor}`; anything not shown is in "
            + $"`nitro agent mail inbox --unread --actor {actor}`.\n"
            + JsonSerializer.Serialize(
                new ListResult<MailMessageDetailResult>(messages),
                JsonSourceGenerationContext.Default.ListResultMailMessageDetailResult);

    private static MailMessageDetailResult TruncateBody(MailMessageDetailResult message, string actor)
    {
        if (message.Body.Length <= MailDigestPolicy.MaxBodyChars)
        {
            return message;
        }

        return message with
        {
            Body = message.Body[..MailDigestPolicy.MaxBodyChars]
                + $"\n[body truncated: nitro agent mail read --message {message.Id} --actor {actor}]"
        };
    }
}
