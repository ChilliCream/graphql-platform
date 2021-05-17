using System.Globalization;

namespace HotChocolate.Data.Neo4J.Language
{
    public class IntegerLiteral : Literal<int>
    {
        public IntegerLiteral(int content) : base(content)
        {
        }

        public override string Print() => Content.ToString(new CultureInfo("en-US"));
    }
}
