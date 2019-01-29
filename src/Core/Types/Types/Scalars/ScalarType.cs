using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// Scalar types represent primitive leaf values in a GraphQL type system.
    /// GraphQL responses take the form of a hierarchical tree;
    /// the leaves on these trees are GraphQL scalars.
    /// </summary>
    public abstract class ScalarType
        : INamedOutputType
        , INamedInputType
        , ISerializableType
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:HotChocolate.Types.ScalarType"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        protected ScalarType(NameString name)
        {
            Name = name.EnsureNotEmpty(nameof(name));
        }

        /// <summary>
        /// Gets the type kind.
        /// </summary>
        public TypeKind Kind { get; } = TypeKind.Scalar;

        /// <summary>
        /// Gets the GraphQL type name of this scalar.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// Gets the optional description of this scalar type.
        /// </summary>
        /// <value></value>
        public virtual string Description { get; }

        /// <summary>
        /// The .net type representation of this scalar.
        /// </summary>
        public abstract Type ClrType { get; }

        /// <summary>
        /// Defines if the specified <paramref name="literal" />
        /// can be parsed by this scalar.
        /// </summary>
        /// <param name="literal">
        /// The literal that shall be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the literal can be parsed by this scalar;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="literal" /> is <c>null</c>.
        /// </exception>
        public abstract bool IsInstanceOfType(IValueNode literal);

        /// <summary>
        /// Defines if the specified <paramref name="value" />
        /// is a instance of this type.
        /// </summary>
        /// <param name="value">
        /// A value representation of this type.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value is a value of this type;
        /// otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsInstanceOfType(object value)
        {
            if (value is null)
            {
                return true;
            }
            return ClrType.IsInstanceOfType(value);
        }

        /// <summary>
        /// Parses the specified <paramref name="literal" />
        /// to the .net representation of this type.
        /// </summary>
        /// <param name="literal">
        /// The literal that shall be parsed.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="literal" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="ScalarSerializationException">
        /// The specified <paramref name="literal" /> cannot be parsed
        /// by this scalar.
        /// </exception>
        public abstract object ParseLiteral(IValueNode literal);

        /// <summary>
        /// Parses the .net value representation to a value literal.
        /// </summary>
        /// <param name="value">
        /// The .net value representation.
        /// </param>
        /// <returns>
        /// Returns a GraphQL literal representing the .net value.
        /// </returns>
        /// <exception cref="ScalarSerializationException">
        /// The specified <paramref name="value" /> cannot be parsed
        /// by this scalar.
        /// </exception>
        public abstract IValueNode ParseValue(object value);

        /// <summary>
        /// Serializes the .net value representation.
        /// </summary>
        /// <param name="value">
        /// The .net value representation.
        /// </param>
        /// <returns>
        /// Returns the serialized value.
        /// </returns>
        /// <exception cref="ScalarSerializationException">
        /// The specified <paramref name="value" /> cannot be serialized
        /// by this scalar.
        /// </exception>
        public abstract object Serialize(object value);

        /// <summary>
        /// Deserializes the serialized value to it`s .net value representation.
        /// </summary>
        /// <param name="serialized">
        /// The serialized value representation.
        /// </param>
        /// <returns>
        /// Returns the .net value representation.
        /// </returns>
        /// <exception cref="ScalarSerializationException">
        /// The specified <paramref name="value" /> cannot be deserialized
        /// by this scalar.
        /// </exception>
        public virtual object Deserialize(object serialized)
        {
            if (TryDeserialize(serialized, out object v))
            {
                return v;
            }

            throw new ScalarSerializationException(
                TypeResources.Scalar_Cannot_Deserialize(Name));
        }


        /// <summary>
        /// Deserializes the serialized value to it`s .net value representation.
        /// </summary>
        /// <param name="value">
        /// The serialized value representation.
        /// </param>
        /// <returns>
        /// Returns the .net value representation.
        /// </returns>
        public abstract bool TryDeserialize(
            object serialized, out object value);
    }
}
