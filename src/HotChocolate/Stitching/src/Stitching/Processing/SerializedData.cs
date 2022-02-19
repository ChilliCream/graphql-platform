namespace HotChocolate.Stitching.Processing;

public sealed class SerializedData
{
    public SerializedData(object? data)
    {
        Data = data;
    }

    public object? Data { get; }
}
