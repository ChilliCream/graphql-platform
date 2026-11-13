using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Transforms <c>@fromContext</c> directives on field arguments into <c>@require</c>
/// directives whose field selection map starts at the context.
/// </summary>
internal static class TransformFromContextToRequire
{
    /// <summary>
    /// Replaces every <c>@fromContext</c> directive in the schema with a <c>@require</c>
    /// directive. The argument keeps its name, type and default value.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        var requireDef = FusionBuiltIns.SourceSchemaDirectives[WellKnownDirectiveNames.Require];

        foreach (var type in schema.Types.OfType<MutableComplexTypeDefinition>())
        {
            foreach (var field in type.Fields)
            {
                foreach (var argument in field.Arguments)
                {
                    var fromContextDirective = argument.Directives.FirstOrDefault(
                        FederationDirectiveNames.FromContext);

                    if (fromContextDirective is null)
                    {
                        continue;
                    }

                    if (!fromContextDirective.Arguments.TryGetValue("field", out var fieldValue)
                        || fieldValue is not StringValueNode fieldString
                        || ToFieldSelectionMap(fieldString.Value) is not { } fieldSelectionMap)
                    {
                        continue;
                    }

                    argument.Directives.Remove(fromContextDirective);
                    argument.Directives.Add(
                        new Directive(
                            requireDef,
                            new ArgumentAssignment("field", fieldSelectionMap)));
                }
            }
        }
    }

    /// <summary>
    /// Converts the <c>field</c> value of a <c>@fromContext</c> directive into a field selection
    /// map.
    /// </summary>
    /// <param name="value">
    /// The <c>field</c> value: a <c>$</c> and a context name, followed by one selection set with a
    /// single chain of fields, or by inline fragments with a type condition that each hold a
    /// single chain of fields.
    /// </param>
    /// <returns>
    /// The field selection map, or <c>null</c> when the value is not in that form.
    /// </returns>
    internal static string? ToFieldSelectionMap(string value)
    {
        if (value.Length < 2 || value[0] != '$' || !IsNameStart(value[1]))
        {
            return null;
        }

        var nameEnd = 2;

        while (nameEnd < value.Length && IsNameContinue(value[nameEnd]))
        {
            nameEnd++;
        }

        var selection = value[nameEnd..].Trim();

        if (selection.Length == 0)
        {
            return null;
        }

        // The selection is parsed as the only definition of a document, so that anything after
        // a first selection set makes the value invalid.
        var source = selection[0] == '{'
            ? "query " + selection
            : "query { " + selection + " }";

        SelectionSetNode selectionSet;

        try
        {
            var document = Utf8GraphQLParser.Parse(source);

            if (document.Definitions is not [OperationDefinitionNode operation])
            {
                return null;
            }

            selectionSet = operation.SelectionSet;
        }
        catch (SyntaxException)
        {
            return null;
        }

        var paths = new List<string>();

        return TryCollectPaths(selectionSet, value[..nameEnd], paths)
            ? string.Join(" | ", paths)
            : null;
    }

    private static bool TryCollectPaths(
        SelectionSetNode selectionSet,
        string prefix,
        List<string> paths)
    {
        switch (selectionSet.Selections)
        {
            case [FieldNode field]:
                return TryCollectFieldPath(field, prefix, paths);

            case [InlineFragmentNode, ..]:
                foreach (var selection in selectionSet.Selections)
                {
                    if (selection is not InlineFragmentNode
                        {
                            TypeCondition: { } typeCondition,
                            Directives.Count: 0,
                            SelectionSet.Selections: [FieldNode]
                        } fragment
                        || !TryCollectPaths(
                            fragment.SelectionSet,
                            $"{prefix}<{typeCondition.Name.Value}>",
                            paths))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return false;
        }
    }

    private static bool TryCollectFieldPath(
        FieldNode field,
        string prefix,
        List<string> paths)
    {
        if (field.Alias is not null || field.Directives.Count > 0)
        {
            return false;
        }

        var path = $"{prefix}.{TransformRequiresToRequire.FormatFieldSelection(field)}";

        if (field.SelectionSet is not null)
        {
            return TryCollectPaths(field.SelectionSet, path, paths);
        }

        paths.Add(path);
        return true;
    }

    private static bool IsNameStart(char c)
        => c is '_' or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z');

    private static bool IsNameContinue(char c)
        => IsNameStart(c) || c is >= '0' and <= '9';
}
