namespace HotChocolate.Fusion.Configuration;

public sealed class FusionMemoryPoolOptions
{
    public int ObjectBatchSize { get; set; } = 64;
    public int DefaultObjectCapacity { get; set; } = 64;
    public int MaxAllowedObjectCapacity { get; set; } = 512;

    public int ListBatchSize { get; set; } = 128;
    public int DefaultListCapacity { get; set; } = 64;
    public int MaxAllowedListCapacity { get; set; } = 512;

    public int LeafFieldBatchSize { get; set; } = 512;
    public int ListFieldBatchSize { get; set; } = 128;
    public int ObjectFieldBatchSize { get; set; } = 128;

    public FusionMemoryPoolOptions Clone()
    {
        return new FusionMemoryPoolOptions
        {
            ObjectBatchSize = ObjectBatchSize,
            DefaultObjectCapacity = DefaultObjectCapacity,
            MaxAllowedObjectCapacity = MaxAllowedObjectCapacity,
            ListBatchSize = ListBatchSize,
            DefaultListCapacity = DefaultListCapacity,
            MaxAllowedListCapacity = MaxAllowedListCapacity,
            LeafFieldBatchSize = LeafFieldBatchSize,
            ListFieldBatchSize = ListFieldBatchSize,
            ObjectFieldBatchSize = ObjectFieldBatchSize
        };
    }
}
