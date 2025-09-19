using CookieCrumble.Formatters;
using SnapshotValueFormatters = CookieCrumble.HotChocolate.Language.Formatters.SnapshotValueFormatters;

namespace CookieCrumble.HotChocolate.Language;

public sealed class CookieCrumbleHotChocolate : SnapshotModule
{
    protected override IEnumerable<ISnapshotValueFormatter> CreateFormatters()
    {
        yield return SnapshotValueFormatters.GraphQLSyntaxNode;
    }
}
