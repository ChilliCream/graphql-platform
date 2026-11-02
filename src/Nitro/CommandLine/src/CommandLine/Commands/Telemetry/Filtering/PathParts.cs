namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal readonly record struct PathParts(string Root, string Leaf, int SegmentCount, bool IsDotted)
{
    public static PathParts Parse(string path)
    {
        var firstDot = path.IndexOf('.');
        var lastDot = path.LastIndexOf('.');
        return new PathParts(
            firstDot < 0 ? string.Empty : path[..firstDot],
            lastDot < 0 ? path : path[(lastDot + 1)..],
            path.Count(static character => character == '.') + 1,
            firstDot >= 0);
    }

    public bool IsSameRoot(PathParts other)
        => IsDotted && other.IsDotted && string.Equals(Root, other.Root, StringComparison.OrdinalIgnoreCase);
}
