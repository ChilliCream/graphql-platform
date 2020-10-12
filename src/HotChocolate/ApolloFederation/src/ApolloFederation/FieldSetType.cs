using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.ApolloFederation.ThrowHelper;

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
    public sealed class FieldSetType
        : ScalarType<SelectionSetNode, StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FieldSetType"/>.
        /// </summary>
        public FieldSetType()
            : this(WellKnownTypeNames.FieldSet)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FieldSetType"/>.
        /// </summary>
        /// <param name="name">
        /// The name the scalar shall have.
        /// </param>
        /// <param name="bind">
        /// Defines if this scalar shall bind implicitly to <see cref="SelectionSetNode"/>.
        /// </param>
        public FieldSetType(NameString name, BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = FederationResources.FieldsetType_Description;
        }

        /// <inheritdoc />
        protected override SelectionSetNode ParseLiteral(StringValueNode valueSyntax)
        {
            try
            {
                return ParseSelectionSet(valueSyntax.Value);
            }
            catch (SyntaxException)
            {
                throw FieldSet_InvalidFormat(this);
            }
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(SelectionSetNode runtimeValue) =>
            new StringValueNode(SerializeSelectionSet(runtimeValue));

        /// <inheritdoc />
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
                return new StringValueNode(SerializeSelectionSet(selectionSet));
            }

            throw Scalar_CannotParseValue(this, resultValue.GetType());
        }

        /// <inheritdoc />
        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is SelectionSetNode selectionSet)
            {
                resultValue = SerializeSelectionSet(selectionSet);
                return true;
            }

            resultValue = null;
            return false;
        }

        /// <inheritdoc />
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

        private static string SerializeSelectionSet(SelectionSetNode selectionSet)
        {
            string s = selectionSet.ToString(false);
            return s.Substring(1, s.Length - 2).Trim();
        }
    }
}
