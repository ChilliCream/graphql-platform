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

        protected override SelectionSetNode ParseLiteral(StringValueNode valueSyntax)
        {
            try
            {
                return ParseSelectionSet(valueSyntax.Value);
            }
            catch (SyntaxException)
            {
                // TODO : ThrowHelper
                throw new SerializationException("The fieldset has an invalid format.", this);
            }
        }

        protected override StringValueNode ParseValue(SelectionSetNode runtimeValue)
        {
            string s = runtimeValue.ToString(false);
            return new StringValueNode(s.Substring(1, s.Length - 2).Trim());
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is string s)
            {
                return new StringValueNode(s);
            }

            if (resultValue is SelectionSetNode selectionSet)
            {
                s = selectionSet.ToString();
                return new StringValueNode(s.Substring(1, s.Length - 2));
            }

            // TODO : throw helper
            throw new SerializationException("Unable to serialize...", this);
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is SelectionSetNode selectionSet)
            {
                string s = selectionSet.ToString(false);
                resultValue = s.Substring(1, s.Length - 2).Trim();
                return true;
            }

            resultValue = null;
            return false;
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is SelectionSetNode selectionSet)
            {
                runtimeValue = selectionSet;
                return true;
            }

            if (resultValue is string serializedSelectionSet)
            {
                try
                {
                    runtimeValue = ParseSelectionSet(serializedSelectionSet);
                    return true;
                }
                catch (SyntaxException)
                {
                    runtimeValue = null;
                    return false;
                }
            }

            runtimeValue = null;
            return false;
        }

        private static SelectionSetNode ParseSelectionSet(string s) =>
            Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{{s}}}");
    }
}
