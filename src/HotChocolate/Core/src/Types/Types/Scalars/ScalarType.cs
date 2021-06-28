using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Scalar types represent primitive leaf values in a GraphQL type system.
    /// GraphQL responses take the form of a hierarchical tree;
    /// the leaves on these trees are GraphQL scalars.
    /// </summary>
    public abstract partial class ScalarType
        : TypeSystemObjectBase
        , ILeafType
        , IHasDirectives
    {
        private Uri? _specifiedBy;

        /// <summary>
        /// Gets the type kind.
        /// </summary>
        public TypeKind Kind { get; } = TypeKind.Scalar;

        /// <summary>
        /// Defines if this scalar binds implicitly to its runtime type or
        /// if it has to be explicitly assigned to it.
        /// </summary>
        public BindingBehavior Bind { get; }

        /// <summary>
        /// The .net type representation of this scalar.
        /// </summary>
        public abstract Type RuntimeType { get; }

        /// <summary>
        /// Gets the optional description of this scalar type.
        /// </summary>
        public Uri? SpecifiedBy
        {
            get => _specifiedBy;
            protected set
            {
                if (IsCompleted)
                {
                    throw new InvalidOperationException(
                        TypeResources.TypeSystemObject_DescriptionImmutable);
                }
                _specifiedBy = value;
            }
        }

        public override IReadOnlyDictionary<string, object?> ContextData => _contextData;

        public IDirectiveCollection Directives { get; private set; }

        public ScalarTypeDefinitionNode? SyntaxNode => null;

        ISyntaxNode? IHasSyntaxNode.SyntaxNode => SyntaxNode;

        public bool IsAssignableFrom(INamedType type) => ReferenceEquals(type, this);

        /// <summary>
        /// Defines if the specified <paramref name="valueSyntax" />
        /// can be parsed by this scalar.
        /// </summary>
        /// <param name="valueSyntax">
        /// The literal that shall be checked.
        /// </param>
        /// <returns>
        /// <c>true</c> if the literal can be parsed by this scalar;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="valueSyntax" /> is <c>null</c>.
        /// </exception>
        public abstract bool IsInstanceOfType(IValueNode valueSyntax);

        /// <summary>
        /// Defines if the specified <paramref name="runtimeValue" />
        /// is a instance of this type.
        /// </summary>
        /// <param name="runtimeValue">
        /// A value representation of this type.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value is a value of this type;
        /// otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsInstanceOfType(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return true;
            }
            return RuntimeType.IsInstanceOfType(runtimeValue);
        }

        /// <summary>
        /// Parses the specified <paramref name="valueSyntax" />
        /// to the .net representation of this type.
        /// </summary>
        /// <param name="valueSyntax">
        /// The literal that shall be parsed.
        /// </param>
        /// <param name="withDefaults">
        /// Can be ignored on leaf types.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="valueSyntax" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="SerializationException">
        /// The specified <paramref name="valueSyntax" /> cannot be parsed
        /// by this scalar.
        /// </exception>
        public abstract object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true);

        /// <summary>
        /// Parses the .net value representation to a value literal.
        /// </summary>
        /// <param name="runtimeValue">
        /// The .net value representation.
        /// </param>
        /// <returns>
        /// Returns a GraphQL literal representing the .net value.
        /// </returns>
        /// <exception cref="SerializationException">
        /// The specified <paramref name="runtimeValue" /> cannot be parsed
        /// by this scalar.
        /// </exception>
        public abstract IValueNode ParseValue(object? runtimeValue);

        /// <summary>
        /// Parses a result value of this into a GraphQL value syntax representation.
        /// </summary>
        /// <param name="resultValue">
        /// A result value representation of this type.
        /// </param>
        /// <returns>
        /// Returns a GraphQL value syntax representation of the <paramref name="resultValue"/>.
        /// </returns>
        /// <exception cref="SerializationException">
        /// Unable to parse the given <paramref name="resultValue"/>
        /// into a GraphQL value syntax representation of this type.
        /// </exception>
        public abstract IValueNode ParseResult(object? resultValue);

        /// <summary>
        /// Serializes the .net value representation.
        /// </summary>
        /// <param name="runtimeValue">
        /// The .net value representation.
        /// </param>
        /// <returns>
        /// Returns the serialized value.
        /// </returns>
        /// <exception cref="SerializationException">
        /// The specified <paramref name="runtimeValue" /> cannot be serialized
        /// by this scalar.
        /// </exception>
        public virtual object? Serialize(object? runtimeValue)
        {
            if (TrySerialize(runtimeValue, out object? s))
            {
                return s;
            }

            throw new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(TypeResourceHelper.Scalar_Cannot_Serialize(Name))
                    .SetExtension("actualValue", runtimeValue?.ToString() ?? "null")
                    .SetExtension("actualType", runtimeValue?.GetType().FullName ?? "null")
                    .Build(),
                this);
        }

        /// <summary>
        /// Tries to serializes the .net value representation to the output format.
        /// </summary>
        /// <param name="runtimeValue">
        /// The .net value representation.
        /// </param>
        /// <param name="resultValue">
        /// The serialized value.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value was correctly serialized; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TrySerialize(object? runtimeValue, out object? resultValue);

        /// <summary>
        /// Deserializes the serialized value to it`s .net value representation.
        /// </summary>
        /// <param name="resultValue">
        /// The serialized value representation.
        /// </param>
        /// <returns>
        /// Returns the .net value representation.
        /// </returns>
        /// <exception cref="SerializationException">
        /// The specified <paramref name="resultValue" /> cannot be deserialized
        /// by this scalar.
        /// </exception>
        public virtual object? Deserialize(object? resultValue)
        {
            if (TryDeserialize(resultValue, out object? v))
            {
                return v;
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_Deserialize(Name),
                this);
        }

        /// <summary>
        /// Tries to deserializes the value from the output format to the .net value representation.
        /// </summary>
        /// <param name="resultValue">
        /// The serialized value.
        /// </param>
        /// <param name="runtimeValue">
        /// The .net value representation.
        /// </param>
        /// <returns>
        /// <c>true</c> if the serialized value was correctly deserialized; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryDeserialize(object? resultValue, out object? runtimeValue);

        protected bool TryConvertSerialized<T>(
            object serialized,
            ValueKind expectedKind,
            out T value)
        {
            if (Scalars.TryGetKind(serialized, out ValueKind kind)
                && kind == expectedKind
                && _converter.TryConvert(serialized, out T c))
            {
                value = c;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
