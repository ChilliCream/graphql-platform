using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See
    /// <a href="https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/RelationshipDetail.html#RelationshipTypes">
    /// Relationship Types
    /// </a>
    /// </summary>
    public class RelationshipTypes : Visitable
    {
        public RelationshipTypes(List<string> values)
        {
            Values = values;
        }

        public override ClauseKind Kind => ClauseKind.RelationshipTypes;

        /// <summary>
        /// The list of types. The types are not escaped and must be escaped prior to rendering.
        /// </summary>
        public List<string> Values { get; }
    }
}
