namespace Mocha.Polymorphic.Contracts;

/// <summary>
/// A command family funnelled through one endpoint, so that ordering holds across its members.
/// </summary>
public interface ILockerCommand
{
    string LockerId { get; }
}

public sealed record DeleteAdmin(string LockerId) : ILockerCommand;

public sealed record CreateAdmin(string LockerId) : ILockerCommand;
