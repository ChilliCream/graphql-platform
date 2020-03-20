namespace HotChocolate.Language
{
    public sealed class SyntaxPrinter
    {

    }

    public sealed class SyntaxSerializer
    {
        public void Serialize(ISyntaxNode node, ISyntaxWriter writer)
        {

        }
    }

    public interface ISyntaxWriter
    {
        void Write();

        void Write(string s);

        void WriteSpace();

        void WriteIndent();
    }
}
