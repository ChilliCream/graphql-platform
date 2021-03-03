using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/RelationshipDetail.html#RelationshipTypes">RelationshipTypes</a>
    /// </summary>
    public class RelationshipTypes : Visitable
    {
        public override ClauseKind Kind { get; } = ClauseKind.RelationshipTypes;
        private readonly List<string> _values;

        public RelationshipTypes(List<string> values) {
            _values = values;
        }

        /// <summary>
        /// The list of types. The types are not escaped and must be escaped prior to rendering.
        /// </summary>
        public List<string> GetValues() => _values;
    }
}
