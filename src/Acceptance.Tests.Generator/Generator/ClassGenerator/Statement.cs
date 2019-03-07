using System;

namespace Generator.ClassGenerator
{
    public class Statement : IClassPart, IEquatable<Statement>
    {
        public static Statement WhenPlaceholder { get; } =
            new Statement("<when>");

        private readonly string _statement;

        public Statement(string statement)
        {
            _statement = statement;
        }

        public string Generate()
        {
            return _statement;
        }

        public override string ToString()
        {
            return Generate();
        }

        public bool Equals(Statement other)
        {
            return string.Equals(_statement, other._statement);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Statement) obj);
        }

        public override int GetHashCode()
        {
            return _statement != null ? _statement.GetHashCode() : 0;
        }

        public static bool operator ==(Statement left, Statement right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Statement left, Statement right)
        {
            return !Equals(left, right);
        }
    }
}
