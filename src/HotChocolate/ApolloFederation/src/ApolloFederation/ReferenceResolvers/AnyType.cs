using System;
using System.Linq;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// The _Any scalar is used to pass representations of entities
    /// from external services into the root _entities field for execution.
    /// </summary>
    public sealed class AnyType: ScalarType<Representation, ObjectValueNode>
    {
        public const string TypeNameField = "__typename";
        public const string DataField = "data";

        /// <summary>
        /// Initializes a new instance of <see cref="AnyType"/>.
        /// </summary>
        public AnyType()
            : this(WellKnownTypeNames.Any)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="AnyType"/>.
        /// </summary>
        /// <param name="name">
        /// The name the scalar shall have.
        /// </param>
        /// <param name="bind">
        /// Defines if this scalar shall bind implicitly to <see cref="SelectionSetNode"/>.
        /// </param>
        public AnyType(NameString name, BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = FederationResources.Any_Description;
        }


        /// <inheritdoc />
        protected override bool IsInstanceOfType(ObjectValueNode valueSyntax)
        {
            return valueSyntax.Fields.Any(field => field.Name.Value == TypeNameField);
        }

        /// <inheritdoc />
        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is Representation representation)
            {
                return ParseValue(representation);
            }

            throw Scalar_CannotParseValue(this, resultValue.GetType());
        }

        /// <inheritdoc />
        protected override Representation ParseLiteral(ObjectValueNode valueSyntax)
        {
            if (valueSyntax.Fields.FirstOrDefault(field => field.Name.Value == "__typename") is {} typename && typename.Value is StringValueNode s)
            {
                return new Representation()
                {
                    Typename = s.Value,
                    Data = valueSyntax
                };
            }

            throw Any_InvalidFormat(this);
        }

        /// <inheritdoc />
        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is Representation)
            {
                resultValue = ParseValue(runtimeValue);
                return true;
            }

            resultValue = null;
            return false;
        }

        /// <inheritdoc />
        protected override ObjectValueNode ParseValue(Representation runtimeValue)
        {
            return new ObjectValueNode(
                runtimeValue.Data.Fields
            );
        }

        /// <inheritdoc />
        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is ObjectValueNode ovn)
            {
                var typeField = ovn.Fields.SingleOrDefault(field => field.Name.Value.Equals(TypeNameField, StringComparison.Ordinal));
                if (typeField is not null && typeField.Value is StringValueNode svn)
                {
                    runtimeValue = new Representation
                    {
                        Data = ovn,
                        Typename = svn.Value
                    };
                    return true;
                }
                runtimeValue = null;
                return false;
            }

            runtimeValue = null;
            return false;
        }
    }
}
