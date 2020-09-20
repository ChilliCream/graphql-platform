using System;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// A scalar called _FieldSet is a custom scalar type that is used to
    /// represent a set of fields.
    ///
    /// Grammatically, a field set is a selection set minus the braces.
    ///
    /// This means it can represent a single field "upc", multiple fields "id countryCode",
    /// and even nested selection sets "id organization { id }".
    /// </summary>
    public class FieldSetType
        : ScalarType<SelectionSetNode, StringValueNode>
    {
        public FieldSetType()
            : base(TypeNames.FieldSet, BindingBehavior.Explicit)
        {
            Description = FederationResources.FieldsetType_Description;
        }

        public FieldSetType(NameString name, BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = FederationResources.FieldsetType_Description;
        }

        protected override SelectionSetNode ParseLiteral(StringValueNode literal)
        {
            try
            {
                return ParseSelectionSet(literal.Value);
            }
            catch (SyntaxException)
            {
                // TODO : ThrowHelper
                throw new SerializationException("The fieldset has an invalid format.", this);
            }
        }

        protected override StringValueNode ParseValue(SelectionSetNode value)
        {
            string s = value.ToString(false);
            return new StringValueNode(s.Substring(1, s.Length - 2).Trim());
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is SelectionSetNode selectionSet)
            {
                string s = selectionSet.ToString();
                return new StringValueNode(s.Substring(1, s.Length - 2));
            }

            // TODO : throw helper
            throw new SerializationException("Unable to serialize...", this);
        }

        public override bool TrySerialize(object? value, out object? serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is SelectionSetNode selectionSet)
            {
                string s = selectionSet.ToString(false);
                serialized = s.Substring(1, s.Length - 2).Trim();
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object? serialized, out object? value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is SelectionSetNode selectionSet)
            {
                value = selectionSet;
                return true;
            }

            if (serialized is string serializedSelectionSet)
            {
                try
                {
                    value = ParseSelectionSet(serializedSelectionSet);
                    return true;
                }
                catch (SyntaxException)
                {
                    value = null;
                    return false;
                }
            }

            value = null;
            return false;
        }

        private static SelectionSetNode ParseSelectionSet(string s) =>
            Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{{s}}}");
    }
}
