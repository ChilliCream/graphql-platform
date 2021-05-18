namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/RelationshipDetail.html">RelationshipDetail</a>.
    /// This is not a public API and just used internally for structuring the tree.
    /// </summary>
    public class RelationshipDetails : Visitable
    {
        private readonly RelationshipLength? _length;

        private RelationshipDetails(
            RelationshipDirection relationshipDirection,
            SymbolicName? symbolicName,
            RelationshipTypes? relationshipTypes,
            RelationshipLength? length,
            Properties? properties)
        {
            Direction = relationshipDirection;
            SymbolicName = symbolicName;
            Types = relationshipTypes;
            _length = length;
            Properties = properties;
        }

        public override ClauseKind Kind => ClauseKind.RelationshipDetails;

        public RelationshipDirection Direction { get; }

        public RelationshipTypes? Types { get; }

        public Properties? Properties { get; }

        public SymbolicName? SymbolicName { get; }

        public bool HasContent() =>
            SymbolicName != null ||
            Types != null ||
            _length != null ||
            Properties != null;

        public RelationshipDetails Named(string newSymbolicName)
        {
            Ensure.IsNotNull(newSymbolicName, "Symbolic name is required.");
            return Named(SymbolicName.Of(newSymbolicName));
        }

        public RelationshipDetails Named(SymbolicName newSymbolicName)
        {
            Ensure.IsNotNull(newSymbolicName, "Symbolic name is required.");
            return new(Direction, newSymbolicName, Types, _length, Properties);
        }

        public RelationshipDetails With(Properties newProperties) =>
            new(Direction, SymbolicName, Types, _length, newProperties);

        public RelationshipDetails Unbounded() =>
            new(Direction, SymbolicName, Types, new RelationshipLength(), Properties);

        public RelationshipDetails Minimum(int? minimum)
        {
            if (minimum == null && _length?.Minimum == null)
            {
                return this;
            }

            RelationshipLength newLength = _length == null
                ? new RelationshipLength(minimum, null)
                : new RelationshipLength(minimum, _length.Maximum);

            return new RelationshipDetails(Direction,
                SymbolicName,
                Types,
                newLength,
                Properties);
        }

        public RelationshipDetails Maximum(int? maximum)
        {
            if (maximum == null && _length?.Maximum == null)
            {
                return this;
            }

            RelationshipLength newLength = _length == null
                ? new RelationshipLength(null, maximum)
                : new RelationshipLength(_length.Minimum, maximum);

            return new RelationshipDetails(Direction,
                SymbolicName,
                Types,
                newLength,
                Properties);
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            SymbolicName?.Visit(cypherVisitor);
            Types?.Visit(cypherVisitor);
            _length?.Visit(cypherVisitor);
            Properties?.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }

        public static RelationshipDetails Create(
            RelationshipDirection direction,
            SymbolicName? symbolicName,
            RelationshipTypes? types) =>
            new(direction, symbolicName, types, null, null);
    }
}
