#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Expresses the length of a relationship.
    /// </summary>
    public class RelationshipLength : Visitable
    {
        public override ClauseKind Kind => ClauseKind.RelationshipLength;
        private readonly int? _minimum;
        private readonly int? _maximum;
        private readonly bool _unbounded;

        public RelationshipLength()
        {
            _minimum = null;
            _maximum = null;
            _unbounded = true;
        }

        public RelationshipLength(int? minimum, int? maximum)
        {
            _minimum = minimum;
            _maximum = maximum;
            _unbounded = false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>Minimum number of hops to match.</returns>
        public int? GetMinimum() =>  _minimum;

        /// <summary>
        ///
        /// </summary>
        /// <returns>Maximum number of hops to match.</returns>
        public int? GetMaximum() => _maximum;

        /// <summary>
        ///
        /// </summary>
        /// <returns>True if neither minimum nor maximum number of hops are set.</returns>
        public bool IsUnbounded() => _unbounded;
    }
}
