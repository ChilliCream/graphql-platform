using System;

namespace Prometheus.Language
{
    public interface ISource
    {
        int Position { get; set; }

        char Read();
        char Peek();
        bool IsEndOfStream();

        bool Test(params int[] expectedCharacters);
        bool Test(params char[] expectedCharacters);

        bool Test(int a, int b);
        bool Test(int a, int b, int c);
        bool Test(char a, char b);
        bool Test(char a, char b, char c);

        char Read(int position);
        string Read(int startIndex, int length);
        bool IsEndOfStream(int position);



        bool Test(int startIndex, params int[] expectedCharacters);
        bool Test(int startIndex, params char[] expectedCharacters);

        bool Test(int startIndex, int a, int b);
        bool Test(int startIndex, int a, int b, int c);
        bool Test(int startIndex, char a, char b);
        bool Test(int startIndex, char a, char b, char c);

        void Reset();
    }

    public interface ILexerContext
    {
        int Position { get; set; }
        int Line { get; set; }
        int LineStart { get; set; }
        int Column { get; set; }

        char ReadPrevious();
        char Read();
        char? Peek();
        bool PeekTest(Func<char, bool> test);
        bool PeekTest(params Func<char, bool>[] test);
        bool PeekTest(params char[] expectedCharacters);

        string Read(int startIndex, int endIndex);
        bool IsEndOfStream();
    }

    public class Source
    {
        private readonly string _body;

        public Source(string body)
        {
            _body = body;
        }

        public char Read(int position)
        {
            return _body[position];
        }

        public string Read(int startIndex, int length)
        {
            return _body.Substring(startIndex, length);
        }

        public bool Check(int startIndex, params int[] expectedCharacters)
        {
            if (_body.Length <= startIndex + expectedCharacters.Length)
            {
                return false;
            }

            for (int i = 0; i < expectedCharacters.Length; i++)
            {
                if (_body[i + startIndex] != expectedCharacters[i])
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsEndOfStream()
        {
            return (position <= _body.Length);
        }
    }
}