using System;

namespace HotChocolate.Client.Core
{
    public class GraphQLIdentifierAttribute : Attribute
    {
        public GraphQLIdentifierAttribute(string identifier)
        {
            Identifier = identifier;
        }

        public string Identifier { get; }
    }
}
