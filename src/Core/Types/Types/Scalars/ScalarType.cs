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
        /// <exception cref="ArgumentException">
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
        public abstract IValueNode ParseValue(object value);

        /// <summary>
        /// Serializes the .net value representation to one of the
        /// following types:
        /// - <see cref="System.String" />
        /// - <see cref="System.Boolean" />
        /// - <see cref="System.Int32" />
        /// - <see cref="System.Double" />
        /// </summary>
        /// <param name="value">
        /// The .net value representation.
        /// </param>
        /// <returns>
        /// Returns the serialized value.
        /// </returns>
        public abstract object Serialize(object value);

        /// <summary>
        /// Deserializes the serialized value to it`s .net value representation.
        /// The <paramref name="serialized" /> can be one of the following types:
        /// - <see cref="System.String" />
        /// - <see cref="System.Boolean" />
        /// - <see cref="System.Int32" />
        /// - <see cref="System.Double" />
        /// </summary>
        /// <param name="serialized">
        /// The serialized value representation.
        /// </param>
        /// <returns>
        /// Returns the .net value representation.
        /// </returns>
        public virtual object Deserialize(object serialized)
        {
            if (TryDeserialize(serialized, out object v))
            {
                return v;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Deserialize(Name));
        }


        /// <summary>
        /// Deserializes the serialized value to it`s .net value representation.
        /// The <paramref name="value" /> can be one of the following types:
        /// - <see cref="System.String" />
        /// - <see cref="System.Boolean" />
        /// - <see cref="System.Int32" />
        /// - <see cref="System.Double" />
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
