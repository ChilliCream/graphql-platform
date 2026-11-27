using HotChocolate.Fusion.Language;
using SelectionMapPathNode = HotChocolate.Fusion.Language.PathNode;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Decides whether a <c>@require</c> map applies to the context values of a query path.
/// </summary>
internal static class ContextRequirementAlternatives
{
    /// <summary>
    /// Determines whether <paramref name="map"/> applies to the values that <paramref name="scope"/> holds for
    /// the contexts of <paramref name="schemaName"/>. A context path with a type condition directly after the
    /// context reference applies when its value has that object type, and every other path applies.
    /// </summary>
    public static bool Applies(
        IValueSelectionNode map,
        ContextScopes contextScopes,
        ContextScopeRef scope,
        string schemaName)
    {
        switch (map)
        {
            case SelectionMapPathNode path:
                return Applies(path, contextScopes, scope, schemaName);

            case ChoiceValueSelectionNode choice:
                foreach (var branch in choice.Branches)
                {
                    if (Applies(branch, contextScopes, scope, schemaName))
                    {
                        return true;
                    }
                }

                return false;

            case ObjectValueSelectionNode objectValue:
                return Applies(objectValue, contextScopes, scope, schemaName);

            case PathObjectValueSelectionNode pathObject:
                return Applies(pathObject.Path, contextScopes, scope, schemaName)
                    && Applies(pathObject.ObjectValueSelection, contextScopes, scope, schemaName);

            case PathListValueSelectionNode pathList:
                return Applies(pathList.Path, contextScopes, scope, schemaName)
                    && Applies(pathList.ListValueSelection, contextScopes, scope, schemaName);

            default:
                return true;
        }
    }

    private static bool Applies(
        SelectionMapPathNode path,
        ContextScopes contextScopes,
        ContextScopeRef scope,
        string schemaName)
    {
        if (path.ContextName is not { } contextName
            || path.TypeName is not { } typeName
            || !contextScopes.TryGetValue(scope, schemaName, contextName.Value, out var value))
        {
            return true;
        }

        return value.ObjectType.Name.Equals(typeName.Value, StringComparison.Ordinal);
    }

    private static bool Applies(
        ObjectValueSelectionNode objectValue,
        ContextScopes contextScopes,
        ContextScopeRef scope,
        string schemaName)
    {
        foreach (var field in objectValue.Fields)
        {
            if (field.ValueSelection is { } valueSelection
                && !Applies(valueSelection, contextScopes, scope, schemaName))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Applies(
        ListValueSelectionNode listValue,
        ContextScopes contextScopes,
        ContextScopeRef scope,
        string schemaName)
        => Applies(listValue.ElementSelection, contextScopes, scope, schemaName);
}
