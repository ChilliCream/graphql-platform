using System;

namespace Prometheus.Language
{
    public interface ILexerContext
    {
        int Position { get; }
        int Line { get; }
        int LineStart { get; }
        int Column { get; }

        void NewLine();
        void NewLine(int lines);
        void UpdateColumn();

        char ReadPrevious();
        char Read();
        char? Peek();
        bool IsEndOfStream();
        ILexerContext Skip();
        ILexerContext Skip(int count);

        bool PeekTest(Func<char, bool> test);
        bool PeekTest(params Func<char, bool>[] test);
        bool PeekTest(params char[] expectedCharacters);

        string Read(int startIndex, int endIndex);
    }
}