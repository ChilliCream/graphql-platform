namespace HotChocolate.Data.TestContext;

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
}
