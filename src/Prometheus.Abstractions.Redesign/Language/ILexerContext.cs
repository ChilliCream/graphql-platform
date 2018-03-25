using System;

namespace Prometheus.Language
{
    public interface ILexerContext
    {
        int Position { get; set; }
        int Line { get; set; }
        int LineStart { get; set; }
        int Column { get; set; }

        void NewLine();

        char ReadPrevious();
        char Read();
        char? Peek();
        bool IsEndOfStream();

        bool PeekTest(Func<char, bool> test);
        bool PeekTest(params Func<char, bool>[] test);
        bool PeekTest(params char[] expectedCharacters);

        string Read(int startIndex, int endIndex);
    }
}