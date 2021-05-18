using System.Globalization;

namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class DoubleLiteral : Literal<double>
    {
        public DoubleLiteral(double content) : base(content)
        {
        }

        public override string Print() => Content.ToString(CultureInfo.InvariantCulture);
    }
}
