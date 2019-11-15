using System;

namespace HotChocolate.Language
{
    public interface IFloatValueLiteral
        : IHasSpan
    {
        float ToSingle();

        double ToDouble();

        Decimal ToDecimal();
    }
}
