namespace HotChocolate.Types;

public class DirectiveLocationTests
{
    // flag values must be set correct
    [Fact]
    public void FlagsCorrect()
    {
        var skip = new HashSet<int>
        {
            (int)DirectiveLocation.Executable,
            (int)DirectiveLocation.TypeSystem,
            (int)DirectiveLocation.Operation,
            (int)DirectiveLocation.Fragment,
        };

        Enum.GetValues(typeof(DirectiveLocation))
            .Cast<DirectiveLocation>()
            .Where(t => skip.Add((int)t))
            .Aggregate(0, (acc, loc) =>
            {
                var v = acc == 0 ? 1 : acc * 2;
                Assert.Equal(v, (int)loc);
                return v;
            });
    }
}
