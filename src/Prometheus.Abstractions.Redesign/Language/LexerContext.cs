using System;

namespace Prometheus.Language
{
    public class LexerContext
        : ILexerContext
    {
        private ISource _source;

        public LexerContext(ISource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
        }

        public int Position { get; set; }
        public int Line { get; set; }
        public int LineStart { get; set; }
        public int Column { get; set; }

        public bool IsEndOfStream()
        {
            return _source.IsEndOfStream(Position);
        }

        public void NewLine()
        {
            Line++;
            LineStart = Position;
        }

        public char? Peek()
        {
            int nextPosition = Position + 1;

            if (_source.IsEndOfStream(nextPosition))
            {
                return null;
            }

            return _source.Read(nextPosition);
        }

        public bool PeekTest(Func<char, bool> test)
        {
            int nextPosition = Position + 1;

            if (_source.IsEndOfStream(nextPosition))
            {
                return false;
            }

            return test(_source.Read(nextPosition));
        }

        public bool PeekTest(params Func<char, bool>[] test)
        {
            int length = test.Length;

            if (_source.IsEndOfStream(Position + length))
            {
                return false;
            }

            for (int i = 0; i <= length; i++)
            {
                char c = _source.Read(Position + 1 + i);
                if (!test[i](c))
                {
                    return false;
                }
            }

            return true;
        }

        public bool PeekTest(params char[] expectedCharacters)
        {
            int length = expectedCharacters.Length;

            if (_source.IsEndOfStream(Position + length))
            {
                return false;
            }

            for (int i = 0; i <= length; i++)
            {
                if (_source.Read(Position + 1 + i) != expectedCharacters[i])
                {
                    return false;
                }
            }

            return true;
        }

        public char Read()
        {
            return _source.Read(++Position);
        }

        public string Read(int startIndex, int endIndex)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startIndex), startIndex,
                    "A source start index cannot be less than 0.");
            }

            if (endIndex <= startIndex)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(endIndex), endIndex,
                    "A source end index cannot be less or equal than the start index.");
            }

            int length = endIndex - startIndex;
            return _source.Read(startIndex, length);
        }

        public char ReadPrevious()
        {
            return _source.Read(Position - 1);
        }

        public void Skip()
        {
            Position++;
        }

        public void Skip(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, 
                    "count mustn't be less than 1.");
            }

            Position += count;
        }
    }
}