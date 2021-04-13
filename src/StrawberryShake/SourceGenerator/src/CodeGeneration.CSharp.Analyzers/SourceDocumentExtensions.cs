using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public static class SourceDocumentExtensions
    {
        public static IEnumerable<SourceDocument> SelectCSharp(
            this IEnumerable<SourceDocument> documents) =>
            documents.Where(t => t.Kind == SourceDocumentKind.CSharp);

        public static IEnumerable<SourceDocument> SelectGraphQL(
            this IEnumerable<SourceDocument> documents) =>
            documents.Where(t => t.Kind == SourceDocumentKind.GraphQL);
    }
}
