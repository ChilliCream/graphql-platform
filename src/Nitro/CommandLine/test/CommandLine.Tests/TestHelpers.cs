using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests;

internal static class TestHelpers
{
    internal static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        IEnumerable<T> items,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
}
