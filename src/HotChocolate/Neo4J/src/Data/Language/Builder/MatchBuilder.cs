using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class MatchBuilder
    {
        private readonly List<PatternElement> _patternList = new ();
        private readonly ConditionBuilder _conditionBuilder;
        private readonly bool _optional;

        public MatchBuilder(bool optional)
        {
            _optional = optional;
        }
    }
}
