namespace HotChocolate.Language;

/// <summary>
/// Represents literals that can be converted to C# float runtime values.
/// </summary>
public interface IFloatValueLiteral : IHasSpan
{
    /// <summary>
    /// Parses the literal as <see cref="float"/> .
    /// </summary>
    float ToSingle();

    /// <summary>
    /// Parses the literal as <see cref="double"/> .
    /// </summary>
    double ToDouble();

    /// <summary>
    /// Parses the literal as <see cref="decimal"/> .
    /// </summary>
    decimal ToDecimal();
}
