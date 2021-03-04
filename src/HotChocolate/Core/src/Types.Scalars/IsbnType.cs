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
            "/^(?:ISBN(?:-10)?:? *)?((?=\\d{1,5}([ -]?)\\d{1,7}\\2?\\d{1,6}\\2?\\d)(?:\\d\\2*){9}[\\dX])$/i" +
            "/^(?:ISBN(?:-13)?:? *)?(97(?:8|9)([ -]?)(?=\\d{1,5}\\2?\\d{1,7}\\2?\\d{1,6}\\2?\\d)(?:\\d\\2*){9}\\d)$/i";

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
