using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public sealed class DirectiveLocation
        : IEquatable<DirectiveLocation?>
    {
        private static readonly Dictionary<string, DirectiveLocation> _cache;

        static DirectiveLocation()
        {
            _cache = GetAll().ToDictionary(t => t._value);
        }

        private readonly string _value;

        private DirectiveLocation(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    Resources.DirectiveLocation_Value_CannotBeNullOrEmpty,
                    nameof(value));
            }

            _value = value;
        }

        public string Value => _value;

        public bool Equals(DirectiveLocation? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other._value.Equals(_value, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as DirectiveLocation);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 17 * _value.GetHashCode();
            }
        }

        public override string ToString()
        {
            return _value;
        }

        public static DirectiveLocation Query { get; } = new("QUERY");

        public static DirectiveLocation Mutation { get; } = new("MUTATION");

        public static DirectiveLocation Subscription { get; } = new("SUBSCRIPTION");

        public static DirectiveLocation Field { get; } = new("FIELD");

        public static DirectiveLocation VariableDefinition { get; } = new("VARIABLE_DEFINITION");

        public static DirectiveLocation FragmentDefinition { get; } = new("FRAGMENT_DEFINITION");

        public static DirectiveLocation FragmentSpread { get; } = new("FRAGMENT_SPREAD");

        public static DirectiveLocation InlineFragment { get; } = new("INLINE_FRAGMENT");

        public static DirectiveLocation Schema { get; } = new("SCHEMA");

        public static DirectiveLocation Scalar { get; } = new("SCALAR");

        public static DirectiveLocation Object { get; } = new("OBJECT");

        public static DirectiveLocation FieldDefinition { get; } = new("FIELD_DEFINITION");

        public static DirectiveLocation ArgumentDefinition { get; } = new("ARGUMENT_DEFINITION");

        public static DirectiveLocation Interface { get; } = new("INTERFACE");

        public static DirectiveLocation Union { get; } = new("UNION");

        public static DirectiveLocation Enum { get; } = new("ENUM");

        public static DirectiveLocation EnumValue { get; } = new("ENUM_VALUE");

        public static DirectiveLocation InputObject { get; } = new("INPUT_OBJECT");

        public static DirectiveLocation InputFieldDefinition { get; } =
            new("INPUT_FIELD_DEFINITION");

        public static bool IsValidName(string value)
        {
            return _cache.ContainsKey(value);
        }

        public static bool TryParse(
            string value,
            out DirectiveLocation? location)
        {
            return _cache.TryGetValue(value, out location);
        }

        private static IEnumerable<DirectiveLocation> GetAll()
        {
            yield return Query;
            yield return Mutation;
            yield return Subscription;
            yield return Field;
            yield return FragmentDefinition;
            yield return FragmentSpread;
            yield return InlineFragment;
            yield return Schema;
            yield return Scalar;
            yield return Object;
            yield return FieldDefinition;
            yield return ArgumentDefinition;
            yield return Interface;
            yield return Union;
            yield return Enum;
            yield return EnumValue;
            yield return InputObject;
            yield return InputFieldDefinition;
        }
    }
}
