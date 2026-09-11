using System.Collections.Concurrent;

namespace HotChocolate.Types.BatchResolvers;

public sealed class BatchProbe
{
    private readonly ConcurrentQueue<Invocation> _invocations = new();

    public IReadOnlyList<Invocation> Invocations => _invocations.ToArray();

    public void Record<T>(string memberName, IEnumerable<T> keys)
        => _invocations.Enqueue(new Invocation(memberName, keys.Cast<object?>().ToArray()));

    public sealed record Invocation(string MemberName, IReadOnlyList<object?> Keys);
}
