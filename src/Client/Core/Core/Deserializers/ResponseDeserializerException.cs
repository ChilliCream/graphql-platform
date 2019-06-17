namespace HotChocolate.Client.Core.Deserializers
{
    public class ResponseDeserializerException : GraphQLException
    {
        public ResponseDeserializerException(string message, int line, int column)
            : base(message)
        {
            Line = line;
            Column = column;
        }

        public int Line { get; }
        public int Column { get; }
    }
}
