namespace HotChocolate.Language;

/// <summary>
/// Represents literals that can be converted to C# integer runtime values.
/// </summary>
public interface IIntValueLiteral : IFloatValueLiteral
{
    /// <summary>
    /// Parses the literal as <see cref="byte"/> .
    /// </summary>
    byte ToByte();

    /// <summary>
    /// Parses the literal as <see cref="short"/> .
    /// </summary>
    short ToInt16();

    /// <summary>
    /// Parses the literal as <see cref="int"/> .
    /// </summary>
    int ToInt32();

    /// <summary>
    /// Parses the literal as <see cref="long"/> .
    /// </summary>
    long ToInt64();

    /// <summary>
    /// Parses the literal as <see cref="sbyte"/> .
    /// </summary>
    sbyte ToSByte();

    /// <summary>
    /// Parses the literal as <see cref="ushort"/> .
    /// </summary>
    ushort ToUInt16();

    /// <summary>
    /// Parses the literal as <see cref="uint"/> .
    /// </summary>
    uint ToUInt32();

    /// <summary>
    /// Parses the literal as <see cref="ulong"/> .
    /// </summary>
    ulong ToUInt64();
}
