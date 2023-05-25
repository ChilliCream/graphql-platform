using System.Collections.Generic;
using System.Collections.ObjectModel;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Handlers;

public static class ProjectedWithExtensions
{
    public const string ProjectedWithKey = "ProjectedWithFields";

    public static ReadOnlyCollection<string>? GetFieldNamesToBeProjectedWith(
        this IReadOnlyDictionary<string, object?> contextData)
    {
        if (contextData.TryGetValue(ProjectedWithKey, out var value))
        {
            return (ReadOnlyCollection<string>) value!;
        }
        return null;
    }

    public static List<string> GetOrCreateFieldNamesToBeProjectedWith(
        this IDictionary<string, object?> contextData)
    {
        if (contextData.TryGetValue(ProjectedWithKey, out var value))
        {
            return (List<string>) value!;
        }
        var list = new List<string>();
        contextData[ProjectedWithKey] = list;
        return list;
    }
}

/// <summary>
/// Not used for now, but this could be useful for computed properties
/// that are computed after data fetching, aka do not end up going to database.
/// </summary>
public sealed class ProjectWithOptimizer : IProjectionOptimizer
{
    public bool CanHandle(ISelection selection)
    {
        return selection.DeclaringType is ObjectType;
    }

    public Selection RewriteSelection(SelectionSetOptimizerContext context, Selection selection)
    {
        var type = (ObjectType) selection.Type;

        int aliasIndex = 0;
        string GetCurrentAlias() => "__required_" + aliasIndex++;

        foreach (var (fieldName, field) in context.Selections)
        {
            var fieldsToProject = field.Field.ContextData.GetFieldNamesToBeProjectedWith();
            if (fieldsToProject is null)
                continue;

            var alias = GetCurrentAlias();
            foreach (var otherFieldName in fieldsToProject)
            {
                if (context.IsFieldAlreadyInSelection(otherFieldName))
                    continue;

                context.AddNewFieldToSelection(selection, type, fieldName, alias);
            }
        }

        return selection;
    }
}
