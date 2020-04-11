namespace HotChocolate.Language.Utilities
{
    public interface ISyntaxWriter
    {
        void Indent();

        void Unindent();

        void Write(char c);

        void Write(string s);

        void WriteLine(bool condition = true);

        void WriteSpace(bool condition = true);

        void WriteIndent(bool condition = true);
    }
}
