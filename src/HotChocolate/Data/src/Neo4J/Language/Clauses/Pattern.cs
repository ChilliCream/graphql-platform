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
    public class Pattern : TypedSubtree<PatternElement, Pattern>
    {
        public Pattern(List<PatternElement> patternElements) : base(patternElements) { }
    }
}