using System.Diagnostics;
using GreenDonut;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class DataLoaderBatchSpan<TKey>(Activity activity) : SpanBase(activity)
{
    public static DataLoaderBatchSpan<TKey>? Start(
        ActivitySource source,
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        var dataLoaderName = dataLoader.GetType().Name;

        var activity = source.StartActivity($"GraphQL DataLoader Batch {dataLoaderName}");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.DataLoaderBatch);

        activity.SetTag(GraphQL.DataLoader.Name, dataLoaderName);
        activity.SetTag(GraphQL.DataLoader.Batch.Size, keys.Count);

        return new DataLoaderBatchSpan<TKey>(activity);
    }
}
