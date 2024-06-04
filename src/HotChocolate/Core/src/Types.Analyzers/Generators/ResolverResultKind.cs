namespace HotChocolate.Types.Analyzers.Generators;

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
