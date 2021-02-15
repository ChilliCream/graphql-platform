namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpDocument
    {
        public CSharpDocument(string name, string source)
        {
            Name = name;
            SourceText = source;
        }

        public string Name { get; }

        public string SourceText { get; }
    }
}
