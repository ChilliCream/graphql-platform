using CookieCrumble.HotChocolate.Formatters;

namespace CookieCrumble.HotChocolate;

public static class CookieCrumbleHotChocolate
{
    public static void Initialize()
    {
        Snapshot.TryRegisterFormatter(SnapshotValueFormatters.ExecutionResult);
        Snapshot.TryRegisterFormatter(SnapshotValueFormatters.GraphQL);
        Snapshot.TryRegisterFormatter(SnapshotValueFormatters.GraphQLHttp);
        Snapshot.TryRegisterFormatter(SnapshotValueFormatters.OperationResult);
        Snapshot.TryRegisterFormatter(SnapshotValueFormatters.Schema);
        Snapshot.TryRegisterFormatter(SnapshotValueFormatters.SchemaError);
    }
}
