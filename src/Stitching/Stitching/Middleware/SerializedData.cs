namespace HotChocolate.Stitching
{
    public sealed class SerializedData
    {
        public SerializedData(object data)
        {
            Data = data;
        }

        public object Data { get; }
    }
}
