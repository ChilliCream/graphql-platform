namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

internal static class SourceDocumentExtensions
{
    public static IEnumerable<GeneratorDocument> SelectCSharp(
        this IEnumerable<GeneratorDocument> documents) =>
        documents.Where(t => t.Kind is GeneratorDocumentKind.CSharp);
}
