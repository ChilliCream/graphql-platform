namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/M15/railroad/RelationshipDetail.html">RelationshipDetail</a>.
    /// This is not a public API and just used internally for structuring the tree.
    /// </summary>
    public class RelationshipDetails : Visitable
    {
        public override ClauseKind Kind { get; } = ClauseKind.RelationshipDetails;
        private readonly RelationshipDirection _direction;
        private readonly SymbolicName _symbolicName;
        private readonly RelationshipTypes _types;
        private readonly RelationshipLength _length;
        private readonly Properties _properties;

        private RelationshipDetails(
            RelationshipDirection relationshipDirection,
            SymbolicName symbolicName,
            RelationshipTypes relationshipTypes,
            RelationshipLength length,
            Properties properties
        )
        {
            _direction = relationshipDirection;
            _symbolicName = symbolicName;
            _types = relationshipTypes;
            _length = length;
            _properties = properties;

        }

        public static RelationshipDetails Create(
            RelationshipDirection direction,
            SymbolicName symbolicName,
            RelationshipTypes types) =>
                new(
                    direction,
                    symbolicName,
                    types,
                    null,
                    null
                );

        public bool HasContent() =>
            _symbolicName != null ||
            _types != null ||
            _length != null ||
            _properties != null;

        public RelationshipDetails Named(string newSymbolicName)
        {
            Ensure.IsNotNull(newSymbolicName, "Symbolic name is required.");
            return Named(SymbolicName.Of(newSymbolicName));
        }

        public RelationshipDetails Named(SymbolicName newSymbolicName)
        {
            Ensure.IsNotNull(newSymbolicName, "Symbolic name is required.");
            return new (_direction, newSymbolicName, _types, _length, _properties);
        }

        public RelationshipDetails With(Properties newProperties) =>
            new(_direction, _symbolicName, _types, _length, newProperties);

        public RelationshipDetails Unbounded() =>
            new(_direction, _symbolicName, _types, new RelationshipLength(), _properties);

        public RelationshipDetails Mininum(int? mininum)
        {
            if (mininum == null && (_length == null || _length.GetMinimum() == null))
            {
                return this;
            }

        }


    }
}
