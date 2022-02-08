using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

public static class SourceDocumentExtensions
{
    public static IEnumerable<GeneratorDocument> SelectCSharp(
        this IEnumerable<GeneratorDocument> documents) =>
        documents.Where(t => t.Kind is GeneratorDocumentKind.CSharp or GeneratorDocumentKind.CSharp);

    public static IEnumerable<GeneratorDocument> SelectGraphQL(
        this IEnumerable<GeneratorDocument> documents) =>
        documents.Where(t => t.Kind == GeneratorDocumentKind.GraphQL);
}
