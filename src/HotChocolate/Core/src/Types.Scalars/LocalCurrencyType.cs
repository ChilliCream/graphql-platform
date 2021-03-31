using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `LocalCurrency` scalar type is a currency string.
    /// </summary>
    public class LocalCurrencyType : ScalarType<DecimalType, StringValueNode>
    {
        private static CultureInfo _cultureInfo = null!;
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
        /// </summary>
        public LocalCurrencyType(
            NameString name,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
        /// </summary>
        public LocalCurrencyType(
            NameString name,
            string? culture,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : this(
                name,
                bind)
        {
            _cultureInfo = CultureInfo.CreateSpecificCulture(culture ?? "en-US");
            Description = description;
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new NotImplementedException();
        }

        protected override DecimalType ParseLiteral(StringValueNode valueSyntax)
        {
            throw new NotImplementedException();
        }

        protected override StringValueNode ParseValue(DecimalType runtimeValue)
        {
            throw new NotImplementedException();
        }
    }
}
