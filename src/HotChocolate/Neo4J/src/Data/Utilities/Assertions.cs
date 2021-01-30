using System;
using HotChocolate.Data.Neo4J.Extensions;

namespace HotChocolate.Data.Neo4J
{
    public static class Assertions
    {
        public static void HasText(string text, string message)
        {
            if(!text.HasText())
            {
                throw new ArgumentException(message);
            }
        }

        public static void IsTrue(bool expression, string message)
        {
            if (!expression)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
