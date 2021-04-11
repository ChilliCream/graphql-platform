using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class UnionQuery : RegularQuery
    {
        public override ClauseKind Kind => ClauseKind.UnionQuery;
        public override Dictionary<string, object> GetParameters()
        {
            throw new System.NotImplementedException();
        }

        public override List<string> GetParameterNames()
        {
            throw new System.NotImplementedException();
        }

        public override string GetCypher()
        {
            throw new System.NotImplementedException();
        }

        private readonly bool _all;
        private readonly SingleQuery _firstQuery;
        private readonly List<UnionPart> _additionalQueries;

        public UnionQuery(bool all, SingleQuery firstQuery, List<UnionPart> additionalQueries)
        {
            _all = all;
            _firstQuery = firstQuery;
            _additionalQueries = additionalQueries;
        }
        //public bool IsAll => _all;
        public static UnionQuery Create(bool unionAll, List<SingleQuery> queries)
        {
            List<UnionPart> unionParts = new List<UnionPart>();
            queries.ForEach(q => unionParts.Add(new UnionPart(unionAll, q)));

            return new UnionQuery(unionAll, queries[0], unionParts);
        }

        public UnionQuery AddAdditionalQueries(List<SingleQuery> newAdditionalQueries)
        {
            List<SingleQuery> queries = new List<SingleQuery>
            {
                _firstQuery
            };
            _additionalQueries.ForEach(q => queries.Add(q.GetQuery()));
            queries.AddRange(newAdditionalQueries);
            return Create(_all, queries);
        }
        public new void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _firstQuery.Visit(cypherVisitor);
            _additionalQueries.ForEach(q => q.Visit(cypherVisitor));
            cypherVisitor.Leave(this);
        }
    }
}
