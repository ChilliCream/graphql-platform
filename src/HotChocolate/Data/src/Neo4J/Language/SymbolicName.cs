using System;
using HotChocolate.Data.Neo4J.Extensions;
using HotChocolate.Data.Neo4J;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A symbolic name to identify nodes, relationships and aliased items.
    /// See <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/SchemaName.html">SchemaName</a>
    /// <a href="https://s3.amazonaws.com/artifacts.opencypher.org/railroad/SymbolicName.html">SymbolicName</a>
    /// </summary>
    public class SymbolicName : Expression
    {
        public override ClauseKind Kind => ClauseKind.SymbolicName;
        private readonly string _value;

        public SymbolicName(string value)
        {
            _value = value;
        }

        public string GetValue() => _value;

        public static SymbolicName Of(string name)
        {
            Assertions.HasText(name, "Name must not be empty.");
            //Assertions.IsTrue();
            return new SymbolicName(name);
        }

        public SymbolicName Concat(string otherValue)
        {
            _ = otherValue ??
                throw new ArgumentNullException(nameof(otherValue));
            if (string.IsNullOrEmpty(otherValue))
            {
                return this;
            }
            return Of(_value + otherValue);
        }

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
            SymbolicName that = (SymbolicName) obj;
            return _value.Equals(that._value);
        }

        // TODO: does not look right come back to this
        public override int GetHashCode() => _value == null ? base.GetHashCode() : _value.GetHashCode();
    }
}
