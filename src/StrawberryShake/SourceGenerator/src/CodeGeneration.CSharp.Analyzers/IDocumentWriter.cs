namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public interface IDocumentWriter
    {
        void WriteDocument(ClientGeneratorContext context, SourceDocument document);

        void Flush();
    }
}
