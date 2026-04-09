using System.Text;

namespace Mocha.Testing;

/// <summary>
/// Assertion extension methods for <see cref="ITrackedMessages"/>.
/// All failures throw <see cref="MessageTrackingException"/> with diagnostic output.
/// </summary>
public static class TrackedMessagesAssertionExtensions
{
    /// <summary>
    /// Asserts that a message of the specified type was published.
    /// </summary>
    /// <typeparam name="T">The expected message type.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    /// <returns>The published message payload.</returns>
    public static T ShouldHavePublished<T>(this ITrackedMessages source)
    {
        return FindDispatched<T>(source, MessageDispatchKind.Published, null, "published");
    }

    /// <summary>
    /// Asserts that a message of the specified type matching a predicate was published.
    /// </summary>
    /// <typeparam name="T">The expected message type.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    /// <param name="predicate">A filter to match the message.</param>
    /// <returns>The published message payload.</returns>
    public static T ShouldHavePublished<T>(this ITrackedMessages source, Func<T, bool> predicate)
    {
        return FindDispatched<T>(source, MessageDispatchKind.Published, predicate, "published");
    }

    /// <summary>
    /// Asserts that a message of the specified type was sent.
    /// </summary>
    /// <typeparam name="T">The expected message type.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    /// <returns>The sent message payload.</returns>
    public static T ShouldHaveSent<T>(this ITrackedMessages source)
    {
        return FindDispatched<T>(source, MessageDispatchKind.Sent, null, "sent");
    }

    /// <summary>
    /// Asserts that a message of the specified type matching a predicate was sent.
    /// </summary>
    /// <typeparam name="T">The expected message type.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    /// <param name="predicate">A filter to match the message.</param>
    /// <returns>The sent message payload.</returns>
    public static T ShouldHaveSent<T>(this ITrackedMessages source, Func<T, bool> predicate)
    {
        return FindDispatched<T>(source, MessageDispatchKind.Sent, predicate, "sent");
    }

    /// <summary>
    /// Asserts that a message of the specified type was consumed.
    /// </summary>
    /// <typeparam name="T">The expected message type.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    /// <returns>The consumed message payload.</returns>
    public static T ShouldHaveConsumed<T>(this ITrackedMessages source)
    {
        return FindInList<T>(source.Consumed, null, "consumed", source);
    }

    /// <summary>
    /// Asserts that a message of the specified type matching a predicate was consumed.
    /// </summary>
    /// <typeparam name="T">The expected message type.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    /// <param name="predicate">A filter to match the message.</param>
    /// <returns>The consumed message payload.</returns>
    public static T ShouldHaveConsumed<T>(this ITrackedMessages source, Func<T, bool> predicate)
    {
        return FindInList<T>(source.Consumed, predicate, "consumed", source);
    }

    /// <summary>
    /// Asserts that a message of the specified type was dispatched (published or sent).
    /// </summary>
    /// <typeparam name="T">The expected message type.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    /// <returns>The dispatched message payload.</returns>
    public static T ShouldHaveDispatched<T>(this ITrackedMessages source)
    {
        return FindInList<T>(source.Dispatched, null, "dispatched", source);
    }

    /// <summary>
    /// Asserts that a message of the specified type matching a predicate was dispatched.
    /// </summary>
    /// <typeparam name="T">The expected message type.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    /// <param name="predicate">A filter to match the message.</param>
    /// <returns>The dispatched message payload.</returns>
    public static T ShouldHaveDispatched<T>(this ITrackedMessages source, Func<T, bool> predicate)
    {
        return FindInList<T>(source.Dispatched, predicate, "dispatched", source);
    }

    /// <summary>
    /// Asserts that no message of the specified type was dispatched.
    /// </summary>
    /// <typeparam name="T">The message type that should not have been dispatched.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    public static void ShouldNotHaveDispatched<T>(this ITrackedMessages source)
    {
        AssertNoneInList<T>(source.Dispatched, "dispatched", source);
    }

    /// <summary>
    /// Asserts that no message of the specified type was consumed.
    /// </summary>
    /// <typeparam name="T">The message type that should not have been consumed.</typeparam>
    /// <param name="source">The tracked messages to search.</param>
    public static void ShouldNotHaveConsumed<T>(this ITrackedMessages source)
    {
        AssertNoneInList<T>(source.Consumed, "consumed", source);
    }

    /// <summary>
    /// Asserts that no messages were tracked (all collections are empty).
    /// </summary>
    /// <param name="source">The tracked messages to search.</param>
    public static void ShouldHaveNoMessages(this ITrackedMessages source)
    {
        if (source.Dispatched.Count == 0 && source.Consumed.Count == 0 && source.Failed.Count == 0)
        {
            return;
        }

        throw new MessageTrackingException(
            "Expected no messages, but found tracked messages.",
            FormatDiagnostic(source));
    }

    private static T FindDispatched<T>(
        ITrackedMessages messages,
        MessageDispatchKind kind,
        Func<T, bool>? predicate,
        string kindName)
    {
        for (var i = 0; i < messages.Dispatched.Count; i++)
        {
            var tracked = messages.Dispatched[i];
            if (tracked.DispatchKind == kind && tracked.Message is T typed)
            {
                if (predicate is null || predicate(typed))
                {
                    return typed;
                }
            }
        }

        throw new MessageTrackingException(
            $"Expected a {typeof(T).Name} message to have been {kindName}, but none was found.",
            FormatDiagnostic(messages));
    }

    private static T FindInList<T>(
        IReadOnlyList<TrackedMessage> list,
        Func<T, bool>? predicate,
        string listName,
        ITrackedMessages messages)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var tracked = list[i];
            if (tracked.Message is T typed)
            {
                if (predicate is null || predicate(typed))
                {
                    return typed;
                }
            }
        }

        throw new MessageTrackingException(
            $"Expected a {typeof(T).Name} message to have been {listName}, but none was found.",
            FormatDiagnostic(messages));
    }

    private static void AssertNoneInList<T>(
        IReadOnlyList<TrackedMessage> list,
        string listName,
        ITrackedMessages messages)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Message is T)
            {
                throw new MessageTrackingException(
                    $"Expected no {typeof(T).Name} message to have been {listName}, but one was found.",
                    FormatDiagnostic(messages));
            }
        }
    }

    private static string FormatDiagnostic(ITrackedMessages messages)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Dispatched:");
        FormatList(sb, messages.Dispatched);

        sb.AppendLine("Consumed:");
        FormatList(sb, messages.Consumed);

        sb.AppendLine("Failed:");
        FormatList(sb, messages.Failed);

        return sb.ToString();
    }

    private static void FormatList(StringBuilder sb, IReadOnlyList<TrackedMessage> list)
    {
        if (list.Count == 0)
        {
            sb.AppendLine("  (none)");
            return;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var msg = list[i];
            sb.Append("  ");
            sb.Append(msg.MessageType.Name);
            if (msg.MessageId is not null)
            {
                sb.Append(" (");
                sb.Append(msg.MessageId);
                sb.Append(')');
            }

            sb.AppendLine();
        }
    }
}
