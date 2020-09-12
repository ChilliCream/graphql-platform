using System;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    public sealed class UrlType
        : ScalarType<Uri, StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UrlType"/> class.
        /// </summary>
        public UrlType()
            : base(ScalarNames.Url, BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlType"/> class.
        /// </summary>
        public UrlType(NameString name)
            : base(name, BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlType"/> class.
        /// </summary>
        public UrlType(NameString name, string description)
            : base(name, BindingBehavior.Implicit)
        {
            Description = description;
        }

        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            return TryParseUri(valueSyntax.Value, out _);
        }

        protected override Uri ParseLiteral(StringValueNode valueSyntax)
        {
            if (TryParseUri(valueSyntax.Value, out Uri uri))
            {
                return uri;
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
                this);
        }

        protected override StringValueNode ParseValue(Uri runtimeValue)
        {
            return new StringValueNode(runtimeValue.AbsoluteUri);
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

            if (resultValue is Uri uri)
            {
                return ParseValue(uri);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
                this);
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is Uri uri)
            {
                resultValue = uri.AbsoluteUri;
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

            if (resultValue is string s && TryParseUri(s, out Uri uri))
            {
                runtimeValue = uri;
                return true;
            }

            if (resultValue is Uri u)
            {
                runtimeValue = u;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        private bool TryParseUri(string value, out Uri uri) =>
            Uri.TryCreate(value, UriKind.Absolute, out uri);
    }
}
