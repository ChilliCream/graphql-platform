using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `LocalTime` scalar type represents a local time string in 24-hr HH:mm[:ss[.SSS]] format.
    /// </summary>
    public class LocalTimeType : ScalarType<DateTimeOffset, StringValueNode>
    {
        private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

        public LocalTimeType()
            : this(
                WellKnownScalarTypes.LocalTime,
                ScalarResources.LocalTimeType_Description,
                BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeType"/> class.
        /// </summary>
        public LocalTimeType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new NotImplementedException();
        }

        protected override DateTimeOffset ParseLiteral(StringValueNode valueSyntax)
        {
            throw new NotImplementedException();
        }

        protected override StringValueNode ParseValue(DateTimeOffset runtimeValue)
        {
            throw new NotImplementedException();
        }
    }
}
