namespace HotChocolate.Language
{
    public interface IFloatValueLiteral
        : IHasSpan
    {
        float ToSingle();

        double ToDouble();

        decimal ToDecimal();
    }
}
