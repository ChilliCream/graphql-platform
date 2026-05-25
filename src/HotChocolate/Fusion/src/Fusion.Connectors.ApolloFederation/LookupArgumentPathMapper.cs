using HotChocolate.Fusion.Language;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Maps a lookup argument's selection to the representation field it binds to.
/// An empty result indicates that the argument's value is itself shaped like
/// the representation and should be merged at its root.
/// </summary>
internal static class LookupArgumentPathMapper
{
    public static string Map(IValueSelectionNode selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return selection switch
        {
            ObjectValueSelectionNode => string.Empty,
            PathNode path => path.PathSegment.FieldName.Value,
            PathObjectValueSelectionNode pathObject => pathObject.Path.PathSegment.FieldName.Value,
            PathListValueSelectionNode pathList => pathList.Path.PathSegment.FieldName.Value,
            _ => throw new InvalidOperationException(
                $"Unsupported lookup-argument selection node '{selection.GetType().Name}'.")
        };
    }
}
