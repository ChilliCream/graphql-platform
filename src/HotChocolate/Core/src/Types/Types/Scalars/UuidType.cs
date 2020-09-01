using System;
using System.Buffers.Text;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    public sealed class UuidType
        : ScalarType<Guid, StringValueNode>
    {
        private readonly string _format;

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType()
            : this('\0')
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType(char format = '\0')
            : this(ScalarNames.Uuid, format)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType(NameString name, char format = '\0')
            : this(name, null, format)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UuidType"/> class.
        /// </summary>
        public UuidType(NameString name, string? description, char format = '\0')
            : base(name, BindingBehavior.Implicit)
        {
            Description = description;
            _format = CreateFormatString(format);
        }

        protected override bool IsInstanceOfType(StringValueNode literal)
        {
            return Utf8Parser.TryParse(literal.AsSpan(), out Guid _, out int _, _format[0]);
        }

        protected override Guid ParseLiteral(StringValueNode literal)
        {
            if (Utf8Parser.TryParse(literal.AsSpan(), out Guid g, out int _, _format[0]))
            {
                return g;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected override StringValueNode ParseValue(Guid value)
        {
            return new StringValueNode(value.ToString(_format));
        }

        public override bool TrySerialize(object? value, out object? serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is Guid uri)
            {
                serialized = uri.ToString(_format);
                return true;
            }

            serialized = null;
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
                throw new ArgumentException(
                    "Unknown format. Guid supports the following format chars: " +
                    $"{{ `N`, `D`, `B`, `P` }}.{Environment.NewLine}" +
                    "https://docs.microsoft.com/en-us/dotnet/api/" +
                    "system.buffers.text.utf8parser.tryparse?" +
                    "view=netcore-3.1#System_Buffers_Text_Utf8Parser_" +
                    "TryParse_System_ReadOnlySpan_System_Byte__System_Guid__" +
                    "System_Int32__System_Char_",
                    nameof(format));
            }

            return format == '\0' ? "N" : format.ToString();
        }
    }
}
