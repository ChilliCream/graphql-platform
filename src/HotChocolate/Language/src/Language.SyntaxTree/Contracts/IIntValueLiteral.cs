namespace HotChocolate.Language
{
    public interface IIntValueLiteral
        : IFloatValueLiteral
    {
        byte ToByte();

        short ToInt16();

        int ToInt32();

        long ToInt64();

        ushort ToUInt16();

        uint ToUInt32();

        ulong ToUInt64();
    }
}
