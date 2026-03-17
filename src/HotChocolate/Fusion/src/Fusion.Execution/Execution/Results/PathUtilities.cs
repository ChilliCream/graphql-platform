namespace HotChocolate.Fusion.Execution.Results;

internal static class PathUtilities
{
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

        var pathSegments = path.ToList();
        var subtreeSegments = subtreeRoot.ToList();

        for (var i = 0; i < subtreeSegments.Count; i++)
        {
            var left = subtreeSegments[i];
            var right = pathSegments[i];

            if (!Equals(left, right))
            {
                return false;
            }
        }

        return true;
    }
}
