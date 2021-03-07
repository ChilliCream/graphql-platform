namespace StrawberryShake.CodeGeneration.CSharp
{
    public class SourceDocument
    {
        public SourceDocument(
            string name,
            string source,
            SourceDocumentKind kind,
            string? hash = null)
        {
            Name = name;
            SourceText = source;
            Kind = kind;
            Hash = hash;
        }

        public string Name { get; }

        public string SourceText { get; }

        public SourceDocumentKind Kind { get; }

        public string? Hash { get; }
    }
}
