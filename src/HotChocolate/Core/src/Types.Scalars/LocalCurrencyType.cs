using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `LocalCurrency` scalar type is a currency string.
    /// </summary>
    public class LocalCurrencyType : ScalarType<decimal, StringValueNode>
    {
        private static CultureInfo _cultureInfo = null!;
        private decimal _inputValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalCurrencyType"/> class.
        /// </summary>
        public LocalCurrencyType()
            : this(
                WellKnownScalarTypes.LocalCurency,
                ScalarResources.LocalCurrencyType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalCurrencyType"/> class.
        /// </summary>
        public LocalCurrencyType(
            NameString name,
            string? description,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalCurrencyType"/> class.
        /// </summary>
        public LocalCurrencyType(
            NameString name,
            decimal input,
            string? culture,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : this(
                name,
                description,
                bind)
        {
            _inputValue = input;
            _cultureInfo = CultureInfo.CreateSpecificCulture(culture ?? "en-US");
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new NotImplementedException();
        }

        protected override decimal ParseLiteral(StringValueNode valueSyntax)
        {
            throw new NotImplementedException();
        }

        protected override StringValueNode ParseValue(decimal runtimeValue)
        {
            throw new NotImplementedException();
        }
    }
}
