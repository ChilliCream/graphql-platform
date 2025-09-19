namespace HotChocolate.Types.Analyzers.Models;

public enum ResolverResultKind
{
    Task,
    Executable,
    Queryable,
    AsyncEnumerable,
    TaskAsyncEnumerable,
    Pure,
    Invalid
}
