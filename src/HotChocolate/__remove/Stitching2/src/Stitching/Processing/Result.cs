namespace HotChocolate.Stitching.Processing
{
    public readonly struct Result
    {
        public Result(NameString typeName)
        {
            TypeName = typeName;
        }

        public NameString TypeName { get; }
    }
}
