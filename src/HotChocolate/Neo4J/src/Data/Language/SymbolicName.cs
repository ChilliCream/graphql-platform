using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A symbolic name to identify nodes, relationships and aliased items.
    /// See <see href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/SchemaName.html">SchemaName</see>
    /// <see href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/SymbolicName.html">SymbolicName</see>
    /// </summary>
    public class SymbolicName : Expression
    {
        public override ClauseKind Kind => ClauseKind.SymbolicName;
        private readonly string _value;

        private SymbolicName(string value)
        {
            _value = value;
        }

        public string GetValue() => _value;

        public static SymbolicName Of(string name)
        {
            Ensure.HasText(name, "Name must not be empty.");
            //Ensure.IsTrue();
            return new SymbolicName(name);
        }

        public static SymbolicName Unresolved() => new (null);

        /// <summary>
        /// Creates a new symbolic name by concatenating {@code otherValue} to this names value.
        /// </summary>
        /// <param name="otherValue"></param>
        /// <returns></returns>
        public SymbolicName Concat(string otherValue)
        {
            Ensure.IsNotNull(otherValue, "Value to concat must not be null.");
            return otherValue == string.Empty ? this : Of(_value + otherValue);
        }

        public static SymbolicName Unsafe(string name) {

            Ensure.HasText(name, "Name must not be empty.");
            return new SymbolicName(name);
        }

        public Condition AsCondition() => new ExpressionCondition(this);

        public MapProjection Project(List<object> entries) => Project(entries.ToArray());

        public MapProjection Project(params object[] entries) => MapProjection.Create(this, entries);

        public override string ToString()
        {
            return _value == null ? "SymbolicName{" +
                "name='" + _value + '\'' +
                '}' : "Unresolved SymbolicName";
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            // Unresolved values are only equal to themselves
            if (_value == null)
            {
                return false;
            }
            var that = (SymbolicName) obj;
            return _value.Equals(that._value);
        }

        public override int GetHashCode() => _value.GetHashCode();
    }
}
