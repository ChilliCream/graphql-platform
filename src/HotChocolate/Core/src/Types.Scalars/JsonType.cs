using System;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class JsonType : ScalarType
    {
        private ITypeConverter _converter = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonType"/> class.
        /// </summary>
        public JsonType(
            NameString name,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonType"/> class.
        /// </summary>
        public JsonType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        public override Type RuntimeType => typeof(JsonDocument);
        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            throw new NotImplementedException();
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new NotImplementedException();
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            throw new NotImplementedException();
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            throw new NotImplementedException();
        }
    }
}
