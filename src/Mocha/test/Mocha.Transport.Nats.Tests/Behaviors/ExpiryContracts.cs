namespace Mocha.Expiry.Contracts;

/// <summary>
/// Published to a stream nothing consumes, so a per-message time to live can be observed expiring.
/// </summary>
public sealed record PerishableNotice(string NoticeId);
