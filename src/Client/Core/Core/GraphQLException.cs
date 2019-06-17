using System;

namespace HotChocolate.Client.Core
{
    public class GraphQLException : Exception
    {
        public GraphQLException()
        {
        }

        public GraphQLException(string message) : base(message)
        {
        }
    }
}
