using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Execution.Projections;

internal static class PropertyTreeBuilder
{
    public static ImmutableArray<PropertyNode> Build(
        SchemaCoordinate fieldCoordinate,
        Type type,
        string requirements)
    {
        if (!requirements.Trim().StartsWith("{"))
        {
            requirements = "{" + requirements + "}";
        }

        var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(requirements);
        return Build(fieldCoordinate, type, selectionSet, Path.Root).ToImmutableArray();
    }

    private static List<PropertyNode> Build(
        SchemaCoordinate fieldCoordinate,
        Type type,
        SelectionSetNode selectionSet,
        Path path)
    {
        var nodes = new List<PropertyNode>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field)
            {
                if(field.Arguments.Count > 0)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("Field arguments in the requirements syntax.")
                            .Build());
                }

                if(field.Directives.Count > 0)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("Field directives in the requirements syntax.")
                            .Build());
                }

                if (field.Alias is not null)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage("Field aliases in the requirements syntax.")
                            .Build());

                }

                var fieldPath = path.Append(field.Name.Value);
                var property = type.GetProperty(field.Name.Value);

                if(property is null)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(
                                "The field requirement `{0}` does not exist on `{1}`.",
                                fieldPath.ToString(),
                                fieldCoordinate.ToString())
                            .Build());
                }

                var children =
                    field.SelectionSet is not null
                        ? Build(fieldCoordinate, property.PropertyType, field.SelectionSet, fieldPath)
                        : null;

                var node = new PropertyNode(property, children);
                nodes.Add(node);
                node.Seal();
            }
            else
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("Only field selections are allowed in the requirements syntax.")
                        .Build());
            }
        }

        return nodes;
    }
}
