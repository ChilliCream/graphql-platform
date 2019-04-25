using System.Collections.Generic;

namespace HotChocolate.Language
{
    public class Utf8ParserContext
    {
        private StringValueNode _description;
        private Stack<SyntaxTokenInfo> _startTokens =
            new Stack<SyntaxTokenInfo>();

        public Utf8ParserContext(ParserOptions options)
        {
            Options = options;
        }

        public ParserOptions Options { get; }

        public void Start(ref Utf8GraphQLReader reader)
        {
            if (!Options.NoLocations)
            {
                var start = new SyntaxTokenInfo(
                    reader.Kind,
                    reader.Start,
                    reader.End,
                    reader.Line,
                    reader.Column);
                _startTokens.Push(start);
            }
        }

        public Location CreateLocation(ref Utf8GraphQLReader reader)
        {
            if (Options.NoLocations)
            {
                return null;
            }

            var end = new SyntaxTokenInfo(
                reader.Kind,
                reader.Start,
                reader.End,
                reader.Line,
                reader.Column);
            return new Location(_startTokens.Pop(), end);
        }

        public void PushDescription(StringValueNode description)
        {
            _description = description;
        }

        public StringValueNode PopDescription()
        {
            StringValueNode description = _description;
            _description = null;
            return description;
        }
    }
}
