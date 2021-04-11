using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/Merge.html
    /// </summary>
    public class Merge : Visitable, IUpdatingClause
    {
        public override ClauseKind Kind => ClauseKind.Match;
        private readonly Pattern _pattern;
        private readonly List<Visitable> _onCreateOrMatchEvents;

        public Merge(Pattern pattern)
        {
            _pattern = pattern;
            _onCreateOrMatchEvents = new List<Visitable>();
        }

        public Merge(Pattern pattern, IEnumerable<MergeAction> mergeActions)
        {
            _pattern = pattern;
            _onCreateOrMatchEvents = new List<Visitable> {_blank};
            _onCreateOrMatchEvents.AddRange(mergeActions);
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _pattern.Visit(cypherVisitor);
            _onCreateOrMatchEvents.ForEach(s => s.Visit(cypherVisitor));
            cypherVisitor.Leave(this);
        }

        private static Literal<string> _blank = new StringLiteral(" ");
    }
}
