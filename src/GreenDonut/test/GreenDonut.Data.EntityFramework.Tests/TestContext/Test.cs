namespace GreenDonut.Data.TestContext;

public class Test
{
    public int Id { get; set; }

    public bool Bool { get; set; }

    public DateOnly DateOnly { get; set; }

    public DateTime DateTime { get; set; }

    public DateTimeOffset DateTimeOffset { get; set; }

    public decimal Decimal { get; set; }

    public double Double { get; set; }

    public float Float { get; set; }

    public Guid Guid { get; set; }

    public int Int { get; set; }

    public long Long { get; set; }

    public short Short { get; set; }

    public string String { get; set; } = "";

    public TimeOnly TimeOnly { get; set; }

    public TimeSpan TimeSpan { get; set; }

    public uint UInt { get; set; }

    public ulong ULong { get; set; }

    public ushort UShort { get; set; }

    public TestByteEnum ByteEnum { get; set; }

    public TestSbyteEnum SbyteEnum { get; set; }

    public TestShortEnum ShortEnum { get; set; }

    public TestUshortEnum UshortEnum { get; set; }

    public TestIntEnum IntEnum { get; set; }

    public TestUintEnum UintEnum { get; set; }

    public TestLongEnum LongEnum { get; set; }

    public TestUlongEnum UlongEnum { get; set; }
}

public enum TestByteEnum : byte
{
    One = 1,
    Two = 2
}

public enum TestSbyteEnum : sbyte
{
    One = 1,
    Two = 2
}

public enum TestShortEnum : short
{
    One = 1,
    Two = 2
}

public enum TestUshortEnum : ushort
{
    One = 1,
    Two = 2
}

public enum TestIntEnum
{
    One = 1,
    Two = 2
}

public enum TestUintEnum : uint
{
    One = 1,
    Two = 2
}

public enum TestLongEnum : long
{
    One = 1,
    Two = 2
}

public enum TestUlongEnum : ulong
{
    One = 1,
    Two = 2
}
