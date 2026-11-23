namespace HotChocolate.Features;

internal static class FeatureCollectionFormatter
{
    public static string Format(this IFeatureCollection features, bool orderByName = false)
    {
        var pairs = orderByName
            ? features.OrderBy(t => t.Key.Name, StringComparer.Ordinal).ToList()
            : features.ToList();
        var lines = new List<string> { $"Revision: {features.Revision}" };

        foreach (var (key, value) in pairs)
        {
            var lookup = features[key];

            lines.Add(
                ReferenceEquals(lookup, value)
                    ? $"{key.Name}: {value}"
                    : $"{key.Name}: {value} (indexer: {lookup ?? "null"})");
        }

        return string.Join('\n', lines);
    }
}
