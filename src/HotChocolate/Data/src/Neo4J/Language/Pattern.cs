using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// <para>
    /// A pattern is something that can be matched. It consists of one or more pattern elements. Those can be nodes or chains
    /// of nodes and relationships.
    /// </para>
    /// <para>See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/Pattern.html">Pattern</a>.</para>
    /// </summary>
    public class Pattern : Visitable// : TypedSubtree<PatternElement, Pattern>
    {
        public override ClauseKind Kind => ClauseKind.Pattern;
        private readonly List<PatternElement> _patternElements;

        public Pattern(List<PatternElement> patternElements)
        {
            _patternElements = patternElements;
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _patternElements.ForEach(element => element.Visit(visitor));
            visitor.Leave(this);
        }
    }
}
