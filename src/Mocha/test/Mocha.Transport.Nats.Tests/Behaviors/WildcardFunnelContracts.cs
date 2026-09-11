namespace Mocha.Wildcard.Commands;

/// <summary>
/// A command family sharing one namespace, so a single wildcard filter covers all of it.
/// </summary>
public interface IWildcardCommand
{
    string Id { get; }
}

public sealed record DeleteThing(string Id) : IWildcardCommand;

public sealed record CreateThing(string Id) : IWildcardCommand;

public sealed record RenameThing(string Id) : IWildcardCommand;
