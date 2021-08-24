namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Expresses the length of a relationship.
    /// </summary>
    public class RelationshipLength : Visitable
    {
        public RelationshipLength()
        {
            Minimum = null;
            Maximum = null;
            IsUnbounded = true;
        }

        public RelationshipLength(int? minimum, int? maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
            IsUnbounded = false;
        }

        public override ClauseKind Kind => ClauseKind.RelationshipLength;

        /// <summary>
        ///
        /// </summary>
        /// <returns>Minimum number of hops to match.</returns>
        public int? Minimum { get; }

        /// <summary>
        ///
        /// </summary>
        /// <returns>Maximum number of hops to match.</returns>
        public int? Maximum { get; }

        /// <summary>
        ///
        /// </summary>
        /// <returns>True if neither minimum nor maximum number of hops are set.</returns>
        public bool IsUnbounded { get;
        }
    }
}
