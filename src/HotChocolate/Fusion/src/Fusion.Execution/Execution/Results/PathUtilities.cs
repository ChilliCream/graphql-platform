using System.Buffers;

namespace HotChocolate.Fusion.Execution.Results;

internal static class PathUtilities
{
    private static readonly ArrayPool<object> s_objectPool = ArrayPool<object>.Shared;

    public static bool IsPathInSubtree(Path path, Path subtreeRoot, bool includeSelf)
    {
        if (path.Length < subtreeRoot.Length
            || (!includeSelf && path.Length == subtreeRoot.Length))
        {
            return false;
        }

        if (subtreeRoot.IsRoot)
        {
            return includeSelf || !path.IsRoot;
        }

        var pathBuffer = s_objectPool.Rent(path.Length);
        var subtreeBuffer = s_objectPool.Rent(subtreeRoot.Length);

        try
        {
            var pathSpan = pathBuffer.AsSpan(0, path.Length);
            var subtreeSpan = subtreeBuffer.AsSpan(0, subtreeRoot.Length);

            path.CopyTo(pathSpan);
            subtreeRoot.CopyTo(subtreeSpan);

            for (var i = 0; i < subtreeSpan.Length; i++)
            {
                if (!Equals(pathSpan[i], subtreeSpan[i]))
                {
                    return false;
                }
            }

            return true;
        }
        finally
        {
            pathBuffer.AsSpan(0, path.Length).Clear();
            s_objectPool.Return(pathBuffer);
            subtreeBuffer.AsSpan(0, subtreeRoot.Length).Clear();
            s_objectPool.Return(subtreeBuffer);
        }
    }
}
