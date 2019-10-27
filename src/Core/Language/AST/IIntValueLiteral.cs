namespace HotChocolate.Language
{
    public interface IIntValueLiteral
        : IFloatValueLiteral
    {
        byte ToByte();

        short ToInt16();

        int ToInt32();

        long ToInt64();
    }
}
