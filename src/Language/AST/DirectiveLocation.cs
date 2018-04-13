using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Language
{
    public sealed class DirectiveLocation
        : IEquatable<DirectiveLocation>
    {
        private string _value;

        private DirectiveLocation(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "The value mustn't be null or empty.",
                    nameof(value));
            }

            _value = value;
        }

        public bool Equals(DirectiveLocation other)
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

            return Equals(obj as DirectiveLocation);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 199 * base.GetHashCode();
            }
        }

        public override string ToString()
        {
            return _value;
        }

        public static DirectiveLocation Query = new DirectiveLocation("QUERY");
        public static DirectiveLocation Mutation = new DirectiveLocation("MUTATION");
        public static DirectiveLocation Subscription = new DirectiveLocation("SUBSCRIPTION");
        public static DirectiveLocation Field = new DirectiveLocation("FIELD");
        public static DirectiveLocation FragmentDefinition = new DirectiveLocation("FRAGMENT_DEFINITION");
        public static DirectiveLocation FragmentSpread = new DirectiveLocation("FRAGMENT_SPREAD");
        public static DirectiveLocation InlineFragment = new DirectiveLocation("INLINE_FRAGMENT");
        public static DirectiveLocation Schema = new DirectiveLocation("SCHEMA");
        public static DirectiveLocation Scalar = new DirectiveLocation("SCALAR");
        public static DirectiveLocation Object = new DirectiveLocation("OBJECT");
        public static DirectiveLocation FieldDefinition = new DirectiveLocation("FIELD_DEFINITION");
        public static DirectiveLocation ArgumentDefinition = new DirectiveLocation("ARGUMENT_DEFINITION");
        public static DirectiveLocation Interface = new DirectiveLocation("INTERFACE");
        public static DirectiveLocation Union = new DirectiveLocation("UNION");
        public static DirectiveLocation Enum = new DirectiveLocation("ENUM");
        public static DirectiveLocation EnumValue = new DirectiveLocation("ENUM_VALUE");
        public static DirectiveLocation InputObject = new DirectiveLocation("INPUT_OBJECT");
        public static DirectiveLocation InputFieldDefinition = new DirectiveLocation("INPUT_FIELD_DEFINITION");

        public static bool IsValidName(string value)
        {
            return DirectiveLocationLookup.IsValidName(value);
        }

        public static bool TryParse(string value, out DirectiveLocation location)
        {
            return DirectiveLocationLookup.TryParse(value, out location);
        }

        private static class DirectiveLocationLookup
        {
            private readonly static Dictionary<string, DirectiveLocation> _locations;

            static DirectiveLocationLookup()
            {
                _locations = GetAll().ToDictionary(t => t._value);
            }

            public static bool IsValidName(string value)
            {
                return _locations.ContainsKey(value);
            }

            public static bool TryParse(string value, out DirectiveLocation location)
            {
                return _locations.TryGetValue(value, out location);
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
}