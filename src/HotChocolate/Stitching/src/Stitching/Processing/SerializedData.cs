namespace HotChocolate.Stitching.Processing;

internal sealed class SerializedData
{
    public SerializedData(object? data)
    {
        Data = data;
    }

    public object? Data { get; }
}
