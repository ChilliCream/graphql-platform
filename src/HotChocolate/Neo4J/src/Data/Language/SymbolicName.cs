namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A symbolic name to identify nodes, relationships and aliased items.
    /// See
    /// <see href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/SchemaName.html">
    /// SchemaName
    /// </see>
    /// <see href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/SymbolicName.html">
    /// SymbolicName
    /// </see>
    /// </summary>
    public class SymbolicName : Expression
    {
        private SymbolicName(string value)
        {
            Value = value;
        }

        public override ClauseKind Kind => ClauseKind.SymbolicName;

        public string Value { get; }

        public static SymbolicName Of(string name)
        {
            Ensure.HasText(name, "Name must not be empty.");
            return new SymbolicName(name);
        }

        public static SymbolicName Unresolved() => new(null);

        public static SymbolicName Unsafe(string name)
        {
            Ensure.HasText(name, "Name must not be empty.");
            return new SymbolicName(name);
        }

        public MapProjection Project(params object[] entries) =>
            MapProjection.Create(this, entries);
    }
}
