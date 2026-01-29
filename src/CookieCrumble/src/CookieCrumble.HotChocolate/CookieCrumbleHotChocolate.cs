using CookieCrumble.Formatters;
using SnapshotValueFormatters = CookieCrumble.HotChocolate.Formatters.SnapshotValueFormatters;

namespace CookieCrumble.HotChocolate;

public sealed class CookieCrumbleHotChocolate : SnapshotModule
{
    protected override IEnumerable<ISnapshotValueFormatter> CreateFormatters()
    {
        yield return SnapshotValueFormatters.ExecutionResult;
        yield return SnapshotValueFormatters.GraphQL;
        yield return SnapshotValueFormatters.GraphQLHttp;
        yield return SnapshotValueFormatters.OperationResult;
        yield return SnapshotValueFormatters.Schema;
        yield return SnapshotValueFormatters.SchemaError;
        yield return SnapshotValueFormatters.Error;
        yield return SnapshotValueFormatters.ResultElement;
    }
}
