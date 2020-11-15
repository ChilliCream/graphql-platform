namespace HotChocolate.Stitching.Delegation
{
    public sealed class SerializedData
    {
        public SerializedData(object? data)
        {
            Data = data;
        }

        public object? Data { get; }
    }
}
