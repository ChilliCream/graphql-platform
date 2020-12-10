using System;
using System.Buffers.Text;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    public sealed class UuidType : ScalarType<Guid, StringValueNode>
    {
        private readonly string _format;

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType() : this('\0')
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType(char format = '\0')
            : this(ScalarNames.Uuid, format: format, bind: BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType(
            NameString name,
            string? description = null,
            char format = '\0',
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
            _format = CreateFormatString(format);
        }

        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            return Utf8Parser.TryParse(valueSyntax.AsSpan(), out Guid _, out _, _format[0]);
        }

        protected override Guid ParseLiteral(StringValueNode valueSyntax)
        {
            if (Utf8Parser.TryParse(valueSyntax.AsSpan(), out Guid g, out _, _format[0]))
            {
                return g;
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
                this);
        }

        protected override StringValueNode ParseValue(Guid runtimeValue)
        {
            return new StringValueNode(runtimeValue.ToString(_format));
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

            if (resultValue is Guid g)
            {
                return ParseValue(g);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, resultValue.GetType()),
                this);
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is Guid uri)
            {
                resultValue = uri.ToString(_format);
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

            if (resultValue is string s && Guid.TryParse(s, out Guid guid))
            {
                runtimeValue = guid;
                return true;
            }

            if (resultValue is Guid)
            {
                runtimeValue = resultValue;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        private static string CreateFormatString(char format)
        {
            if (format != '\0'
                && format != 'N'
                && format != 'D'
                && format != 'B'
                && format != 'P')
            {
                throw new ArgumentException(TypeResources.UuidType_FormatUnknown, nameof(format));
            }

            return format == '\0' ? "N" : format.ToString();
        }
    }
}
