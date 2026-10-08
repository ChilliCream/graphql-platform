namespace HotChocolate.Language.SyntaxTree;

public static class ConcurrentReadHelper
{
    /// <summary>
    /// Creates <paramref name="count"/> nodes, reads each node from two threads at the same time
    /// and returns the distinct results.
    /// </summary>
    public static IReadOnlyList<string> ReadConcurrently<TNode>(
        Func<TNode> create,
        Func<TNode, string> read,
        int count = 200_000)
    {
        var nodes = new TNode[count];

        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i] = create();
        }

        var results = new string[2][];
        using var barrier = new Barrier(2);
        var threads = new Thread[2];

        for (var t = 0; t < threads.Length; t++)
        {
            var threadResults = results[t] = new string[count];
            threads[t] = new Thread(() =>
            {
                barrier.SignalAndWait();

                for (var i = 0; i < nodes.Length; i++)
                {
                    threadResults[i] = read(nodes[i]);
                }
            });
            threads[t].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        return results.SelectMany(r => r).Distinct().Order(StringComparer.Ordinal).ToArray();
    }
}
