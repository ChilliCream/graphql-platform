namespace HotChocolate.Language
{
    public class Utf8ParserContext
    {
        public void Start(in Utf8GraphQLReader reader)
        {
            // use stack for token info
        }

        public Location CreateLocation(in Utf8GraphQLReader reader)
        {

        }

        public StringValueNode Description { get; set; }
    }
}
