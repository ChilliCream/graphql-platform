using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Contracts;
using HotChocolate.Stitching.Types.Directives;

namespace HotChocolate.Stitching.Types.Extensions;

public static class NavigatorExtensions
{
    public static SchemaCoordinate CreateCoordinate(this ISyntaxNavigator navigator, ISyntaxNode node)
    {
        var namedSyntaxNodes = GetCoordinateNames(navigator, node)
            .ToList();

        switch (namedSyntaxNodes.Count)
        {
            case 1:
                return new SchemaCoordinate(new NameString(namedSyntaxNodes[0].Value));

            case 2:
                return new SchemaCoordinate(
                    new NameString(namedSyntaxNodes[1].Value),
                    new NameString(namedSyntaxNodes[0].Value));
            case 3:
                return new SchemaCoordinate(
                    new NameString(namedSyntaxNodes[2].Value),
                    new NameString(namedSyntaxNodes[1].Value),
                    new NameString(namedSyntaxNodes[0].Value));
        }

        return new SchemaCoordinate();
    }

    private static IEnumerable<NameNode> GetCoordinateNames(ISyntaxNavigator navigator, ISyntaxNode node)
    {
        var nodes = new List<ISyntaxNode> { node };
        nodes.AddRange(navigator.GetAncestors<INamedSyntaxNode>());

        foreach (ISyntaxNode syntaxNode in nodes)
        {
            if (syntaxNode is not INamedSyntaxNode namedSyntaxNode)
            {
                continue;
            }

            NameNode defaultName = namedSyntaxNode.Name;
            if (!namedSyntaxNode.TryGetSource(out SourceDirective? sourceDirective))
            {
                yield return defaultName;
                continue;
            }

            SchemaCoordinateNode schemaCoordinateNode = sourceDirective.Coordinate
                .ToSyntax();

            IEnumerable<ISyntaxNode> coordinateNodes = schemaCoordinateNode.GetNodes();
            foreach (ISyntaxNode coordinateNode in coordinateNodes)
            {
                if (coordinateNode is NameNode nameNode)
                {
                    yield return nameNode;
                }
            }
        }
    }
}
