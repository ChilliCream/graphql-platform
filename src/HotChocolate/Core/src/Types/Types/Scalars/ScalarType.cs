using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Scalar types represent primitive leaf values in a GraphQL type system.
    /// GraphQL responses take the form of a hierarchical tree;
    /// the leaves on these trees are GraphQL scalars.
    /// </summary>
    public abstract class ScalarType
        : TypeSystemObjectBase
        , ILeafType
        , IHasDirectives
    {
        private readonly ITypeConversion _converter = TypeConversion.Default;
        private readonly ExtensionData _contextData = new ExtensionData();

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:HotChocolate.Types.ScalarType"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        protected ScalarType(NameString name, BindingBehavior bind = BindingBehavior.Explicit)
        {
            Name = name.EnsureNotEmpty(nameof(name));
            Bind = bind;

            Directives = default!;
        }

        ISyntaxNode? IHasSyntaxNode.SyntaxNode { get; }

        /// <summary>
        /// Gets the type kind.
        /// </summary>
        public TypeKind Kind { get; } = TypeKind.Scalar;

        public BindingBehavior Bind { get; }

        /// <summary>
        /// The .net type representation of this scalar.
        /// </summary>
        public abstract Type RuntimeType { get; }

        public override IReadOnlyDictionary<string, object?> ContextData => _contextData;

        public IDirectiveCollection Directives { get; private set; }

        public ISyntaxNode? SyntaxNode => null;

        public bool IsAssignableFrom(INamedType type) => ReferenceEquals(type, this);

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
        public virtual bool IsInstanceOfType(object? value)
        {
            if (value is null)
            {
                return true;
            }
            return RuntimeType.IsInstanceOfType(value);
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
        public abstract object? ParseLiteral(IValueNode literal);

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
        public abstract IValueNode ParseValue(object? value);

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
        public virtual object? Serialize(object? value)
        {
            if (TrySerialize(value, out object? s))
            {
                return s;
            }

            throw new ScalarSerializationException(
                ErrorBuilder.New()
                    .SetMessage(TypeResourceHelper.Scalar_Cannot_Serialize(Name))
                    .SetExtension("actualValue", value?.ToString() ?? "null")
                    .SetExtension("actualType", value?.GetType().FullName ?? "null")
                    .Build());
        }

        /// <summary>
        /// Tries to serializes the .net value representation to the output format.
        /// </summary>
        /// <param name="value">
        /// The .net value representation.
        /// </param>
        /// <param name="serialized">
        /// The serialized value.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value was correctly serialized; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TrySerialize(object? value, out object? serialized);

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
        public virtual object? Deserialize(object? serialized)
        {
            if (TryDeserialize(serialized, out object v))
            {
                return v;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_Deserialize(Name));
        }


        /// <summary>
        /// Tries to deserializes the value from the output format to the .net value representation.
        /// </summary>
        /// <param name="serialized">
        /// The serialized value.
        /// </param>
        /// <param name="value">
        /// The .net value representation.
        /// </param>
        /// <returns>
        /// <c>true</c> if the serialized value was correctly deserialized; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryDeserialize(object? serialized, out object? value);

        internal sealed override void Initialize(ITypeDiscoveryContext context)
        {
            context.Interceptor.OnBeforeRegisterDependencies(context, null, _contextData);
            OnRegisterDependencies(context, _contextData);
            context.Interceptor.OnAfterRegisterDependencies(context, null, _contextData);
            base.Initialize(context);
        }

        protected virtual void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            IDictionary<string, object?> contextData)
        {
        }

        internal sealed override void CompleteName(ITypeCompletionContext context)
        {
            context.Interceptor.OnBeforeCompleteName(context, null, _contextData);
            OnCompleteName(context, _contextData);
            base.CompleteName(context);
            context.Interceptor.OnAfterCompleteName(context, null, _contextData);
        }

        protected virtual void OnCompleteName(
            ITypeCompletionContext context,
            IDictionary<string, object?> contextData)
        {
        }

        internal sealed override void CompleteType(ITypeCompletionContext context)
        {
            context.Interceptor.OnBeforeCompleteType(context, null, _contextData);
            OnCompleteType(context, _contextData);
            base.CompleteType(context);
            context.Interceptor.OnAfterCompleteType(context, null, _contextData);
        }

        protected virtual void OnCompleteType(
            ITypeCompletionContext context,
            IDictionary<string, object?> contextData)
        {
            Directives = DirectiveCollection.CreateAndComplete(
                context, this, Array.Empty<DirectiveDefinition>());
        }

        protected bool TryConvertSerialized<T>(
            object serialized,
            ValueKind expectedKind,
            out T value)
        {
            if (Scalars.TryGetKind(serialized, out ValueKind kind)
                && kind == expectedKind
                && _converter.TryConvert<object, T>(serialized, out T c))
            {
                value = c;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
