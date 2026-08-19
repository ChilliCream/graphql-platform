using System.Globalization;

namespace Mocha.Transport.Nats;

/// <summary>
/// Header names and subject conventions for JetStream's scheduled and expiring messages.
/// </summary>
public static class NatsScheduling
{
    /// <summary>
    /// The token appended to a subject to form the namespace scheduled messages are published to.
    /// </summary>
    /// <remarks>
    /// A separate subject is required: the server rejects a schedule whose target subject is the
    /// same subject the scheduling message arrived on.
    /// </remarks>
    public const string SchedulingSuffix = "_schedule";

    /// <summary>
    /// Header carrying when a scheduled message should be delivered. Requires server 2.12.
    /// </summary>
    public const string ScheduleHeader = "Nats-Schedule";

    /// <summary>
    /// Header carrying the subject a scheduled message is finally delivered to. Requires server 2.12.
    /// </summary>
    public const string ScheduleTargetHeader = "Nats-Schedule-Target";

    /// <summary>
    /// Header carrying a per-message time to live. Requires server 2.11.
    /// </summary>
    public const string TtlHeader = "Nats-TTL";

    /// <summary>
    /// Gets the subject one scheduled message is published to.
    /// </summary>
    /// <param name="subject">The subject the message is finally delivered to.</param>
    /// <param name="scheduleId">An identifier unique to this scheduled message.</param>
    /// <returns>The scheduling subject.</returns>
    /// <remarks>
    /// The identifier is part of the subject because a subject holds at most one schedule. Deriving
    /// the subject from the target alone would make each scheduled message replace the last one aimed
    /// at the same target, and the replaced message is never delivered.
    /// </remarks>
    public static string ToSchedulingSubject(string subject, string scheduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);

        return $"{subject}.{SchedulingSuffix}.{scheduleId}";
    }

    /// <summary>
    /// Gets the subject filter covering every scheduled message for the specified subject.
    /// </summary>
    /// <param name="subject">The subject the messages are finally delivered to.</param>
    /// <returns>The subject filter a stream captures to hold scheduled messages.</returns>
    public static string ToSchedulingFilter(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        return $"{subject}.{SchedulingSuffix}.>";
    }

    /// <summary>
    /// Creates an identifier for one scheduled message, safe to use as a subject token.
    /// </summary>
    /// <returns>The identifier.</returns>
    public static string NewScheduleId() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Formats an instant as a one-shot schedule value.
    /// </summary>
    /// <param name="deliverAt">When the message should be delivered.</param>
    /// <returns>The header value, for example <c>@at 2026-08-11T09:00:00.0000000Z</c>.</returns>
    public static string ToScheduleValue(DateTimeOffset deliverAt)
        => $"@at {deliverAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Formats a time to live as a duration value.
    /// </summary>
    /// <param name="timeToLive">How long the message remains deliverable.</param>
    /// <returns>The header value, for example <c>30s</c>.</returns>
    public static string ToTtlValue(TimeSpan timeToLive)
    {
        var seconds = (long)Math.Ceiling(timeToLive.TotalSeconds);

        return $"{Math.Max(seconds, 1).ToString(CultureInfo.InvariantCulture)}s";
    }
}
