namespace HotChocolate.Data.Projections;

internal sealed class ProjectionRequirements(string[] memberPaths)
{
    public ReadOnlySpan<string> MemberPaths => memberPaths;
}
