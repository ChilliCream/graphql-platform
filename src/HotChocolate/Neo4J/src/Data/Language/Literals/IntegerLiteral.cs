using System;
using System.Globalization;

namespace HotChocolate.Data.Neo4J.Language
{
    public class IntegerLiteral : Literal<int>
    {
        public IntegerLiteral(int content) : base(content) { }
        public override string AsString() => Convert.ToString(GetContent(), new CultureInfo("en-US"));
    }
}