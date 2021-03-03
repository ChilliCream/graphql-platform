using System;

namespace HotChocolate.Data.Neo4J.Exceptions
{
    public class Neo4JException : Exception
    {
        public Neo4JException(string message) : base(message) { }
    }
}
