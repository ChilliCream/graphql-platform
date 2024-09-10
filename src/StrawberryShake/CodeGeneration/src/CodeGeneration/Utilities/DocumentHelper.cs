using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using SyntaxVisitor = HotChocolate.Language.Visitors.SyntaxVisitor;

namespace StrawberryShake.CodeGeneration.Utilities;

public static class DocumentHelper
{
    public static IReadOnlyList<GraphQLFile> GetTypeSystemDocuments(
        this IEnumerable<GraphQLFile> documentNodes)
    {
        if (documentNodes is null)
        {
            throw new ArgumentNullException(nameof(documentNodes));
        }

        return documentNodes.Where(doc =>
                doc.Document.Definitions.All(def =>
                    def is ITypeSystemDefinitionNode or ITypeSystemExtensionNode))
            .ToList();
    }

    public static IReadOnlyList<GraphQLFile> GetExecutableDocuments(
        this IEnumerable<GraphQLFile> documentNodes)
    {
        if (documentNodes is null)
        {
            throw new ArgumentNullException(nameof(documentNodes));
        }

        return documentNodes.Where(doc =>
                doc.Document.Definitions.All(def => def is IExecutableDefinitionNode))
            .ToList();
    }

    public static void IndexSyntaxNodes(
        IEnumerable<GraphQLFile> files,
        IDictionary<ISyntaxNode, string> lookup)
    {
        foreach (var file in files)
        {
            IndexSyntaxNodes(file, lookup);
        }
    }

    public static void IndexSyntaxNodes(
        GraphQLFile file,
        IDictionary<ISyntaxNode, string> lookup)
    {
        SyntaxVisitor
            .Create(
                enter: node =>
                {
                    if (!lookup.ContainsKey(node))
                    {
                        lookup.Add(node, file.FileName);
                    }
                    return SyntaxVisitor.Continue;
                },
                defaultAction: SyntaxVisitor.Continue,
                options: new SyntaxVisitorOptions
                {
                    VisitArguments = true,
                    VisitDescriptions = true,
                    VisitDirectives = true,
                    VisitNames = true,
                })
            .Visit(file.Document);
    }
}
