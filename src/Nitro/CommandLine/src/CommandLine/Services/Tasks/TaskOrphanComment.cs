namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A comment row identified by its task and comment ID.
/// </summary>
internal sealed record TaskOrphanComment(string TaskId, long CommentId);
