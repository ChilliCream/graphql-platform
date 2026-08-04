namespace HotChocolate.Fusion.Suites.ProvidesOnlyRequestedFields.SubgraphA;

internal static class SubgraphAData
{
    public static readonly IReadOnlyDictionary<string, Entity> EntitiesById =
        new Dictionary<string, Entity>(StringComparer.Ordinal)
        {
            ["e1"] = new Entity
            {
                Id = "e1",
                Name = "Entity One",
                Description = "Description One",
                Extra = "Extra One"
            },
            ["e2"] = new Entity
            {
                Id = "e2",
                Name = "Entity Two",
                Description = "Description Two",
                Extra = "Extra Two"
            }
        };
}
