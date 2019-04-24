namespace HotChocolate.Language
{
    public class Utf8ParserContext
    {
        private StringValueNode _description;

        public Utf8ParserContext(ParserOptions options)
        {
            Options = options;
        }

        public ParserOptions Options { get; }

        public void Start(in Utf8GraphQLReader reader)
        {
            // use stack for token info
        }

        public Location CreateLocation(in Utf8GraphQLReader reader)
        {
            return null;
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
