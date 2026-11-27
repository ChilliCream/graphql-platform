using HotChocolate.Fusion.Language;
using HotChocolate.Types.Mutable;
using SelectionMapPathNode = HotChocolate.Fusion.Language.PathNode;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// An argument of a field whose <c>@require</c> map reads a context.
/// </summary>
/// <param name="ArgumentName">The name of the argument.</param>
/// <param name="IsNonNull">Whether the type of the argument is non-null.</param>
/// <param name="Map">The text of the map.</param>
/// <param name="Value">The parsed map.</param>
internal sealed record ContextRequirementArgument(
    string ArgumentName,
    bool IsNonNull,
    string Map,
    IValueSelectionNode Value);

/// <summary>
/// Decides whether a <c>@require</c> map applies to the context values of a query path.
/// </summary>
internal static class ContextRequirementAlternatives
{
    /// <summary>
    /// Determines whether <paramref name="map"/> applies to the values that <paramref name="scope"/> holds for
    /// the contexts of <paramref name="schemaName"/>. A context path with a type condition directly after the
    /// context reference applies when the object type of its value is a possible type of that condition,
    /// and every other path applies.
    /// </summary>
    public static bool Applies(
        IValueSelectionNode map,
        MutableSchemaDefinition schema,
        ContextScopes contextScopes,
        ContextScopeRef scope,
        string schemaName)
    {
        switch (map)
        {
            case SelectionMapPathNode path:
                return Applies(path, schema, contextScopes, scope, schemaName);

            case ChoiceValueSelectionNode choice:
                foreach (var branch in choice.Branches)
                {
                    if (Applies(branch, schema, contextScopes, scope, schemaName))
                    {
                        return true;
                    }
                }

                return false;

            case ObjectValueSelectionNode objectValue:
                return Applies(objectValue, schema, contextScopes, scope, schemaName);

            case PathObjectValueSelectionNode pathObject:
                return Applies(pathObject.Path, schema, contextScopes, scope, schemaName)
                    && Applies(pathObject.ObjectValueSelection, schema, contextScopes, scope, schemaName);

            case PathListValueSelectionNode pathList:
                return Applies(pathList.Path, schema, contextScopes, scope, schemaName)
                    && Applies(pathList.ListValueSelection, schema, contextScopes, scope, schemaName);

            default:
                return true;
        }
    }

    private static bool Applies(
        SelectionMapPathNode path,
        MutableSchemaDefinition schema,
        ContextScopes contextScopes,
        ContextScopeRef scope,
        string schemaName)
    {
        if (path.ContextName is not { } contextName
            || path.TypeName is not { } typeName
            || !contextScopes.TryGetValue(scope, schemaName, contextName.Value, out var value)
            || !schema.Types.TryGetType(typeName.Value, out var conditionType))
        {
            return true;
        }

        return schema.GetPossibleTypes(conditionType).Contains(value.ObjectType);
    }

    private static bool Applies(
        ObjectValueSelectionNode objectValue,
        MutableSchemaDefinition schema,
        ContextScopes contextScopes,
        ContextScopeRef scope,
        string schemaName)
    {
        foreach (var field in objectValue.Fields)
        {
            if (field.ValueSelection is { } valueSelection
                && !Applies(valueSelection, schema, contextScopes, scope, schemaName))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Applies(
        ListValueSelectionNode listValue,
        MutableSchemaDefinition schema,
        ContextScopes contextScopes,
        ContextScopeRef scope,
        string schemaName)
        => Applies(listValue.ElementSelection, schema, contextScopes, scope, schemaName);
}
