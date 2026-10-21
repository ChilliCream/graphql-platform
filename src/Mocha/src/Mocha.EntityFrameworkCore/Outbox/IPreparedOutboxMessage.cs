namespace Mocha.EntityFrameworkCore;

/// <summary>
/// An independent message snapshot awaiting attachment to a DbContext.
/// </summary>
public interface IPreparedOutboxMessage : IDisposable;
