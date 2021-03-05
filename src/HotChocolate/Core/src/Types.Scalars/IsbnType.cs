using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    public class IsbnType : RegexType
    {
        /// <summary>
        /// The `ISBN` scalar type is a ISBN-10 or ISBN-13 number: https://en.wikipedia.org/wiki/International_Standard_Book_Number.
        /// </summary>

        private static readonly string _validationPattern =
            "^(?:ISBN(-1(?:(0)|3))?:?\\ )?" +
            "(?(1)(?(2)(?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0-9X]{13}$)[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X]" +
            "|(?=[0-9]{13}$|(?=(?:[0-9]+[- ]){4})[- 0-9]{17}$)97[89][- ]?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9]"  +
            ")|(?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0-9X]{13}$|97[89][0-9]{10}$|(?=(?:[0-9]+[- ]){4})[- 0-9]{17}$)" +
            "(?:97[89][- ]?)?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X])$";

        /// <summary>
        /// Initializes a new instance of the <see cref="IsbnType"/> class.
        /// </summary>
        public IsbnType()
            : base(WellKnownScalarTypes.Isbn,
                _validationPattern,
                ScalarResources.IsbnType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        protected override Exception CreateParseLiteralError(StringValueNode valueSyntax)
        {
            return ThrowHelper.IsbnType_ParseLiteral_IsInvalid(this);
        }

        protected override Exception CreateParseValueError(string runtimeValue)
        {
            return ThrowHelper.IsbnType_ParseValue_IsInvalid(this);
        }
    }
}
