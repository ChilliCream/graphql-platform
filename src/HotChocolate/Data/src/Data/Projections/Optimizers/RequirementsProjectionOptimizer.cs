using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Requirements;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Projections.Optimizers;

/// <summary>
/// Adds the required CLR member paths to each field selection.
/// </summary>
public sealed class RequirementsProjectionOptimizer : IProjectionOptimizer
{
    public bool CanHandle(Selection selection)
        => (selection.Field.Flags & CoreFieldFlags.WithRequirements) == CoreFieldFlags.WithRequirements;

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        if (context.Schema.Features.TryGet(out FieldRequirementsMetadata? metadata)
            && metadata.GetRequirements(selection.Field) is { } requirements)
        {
            var paths = new List<string>();
            CollectPaths(requirements.Nodes, null, paths);
            context.CreateSelectionFeatures(selection).SetSafe(new ProjectionRequirements([.. paths]));
        }

        return selection;
    }

    private static void CollectPaths(
        IReadOnlyList<PropertyNode> requirements,
        string? parentPath,
        List<string> paths)
    {
        foreach (var requirement in requirements)
        {
            var path = parentPath is null
                ? requirement.Property.Name
                : parentPath + "." + requirement.Property.Name;

            if (RequiresWholeMember(requirement))
            {
                paths.Add(path);
                continue;
            }

            foreach (var typeNode in requirement.Nodes)
            {
                CollectPaths(typeNode.Nodes, path, paths);
            }
        }
    }

    private static bool RequiresWholeMember(PropertyNode requirement)
    {
        if (requirement.Nodes.Count == 0)
        {
            return true;
        }

        foreach (var typeNode in requirement.Nodes)
        {
            if (typeNode.Nodes.Count == 0)
            {
                return true;
            }
        }

        return false;
    }

    public static RequirementsProjectionOptimizer Create(ProjectionProviderContext _) => new();
}
