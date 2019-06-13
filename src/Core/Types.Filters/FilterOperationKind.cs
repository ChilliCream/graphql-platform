namespace HotChocolate.Types.Filters
{
    public enum FilterOperationKind
        : byte
    {
        Equals = 0x0000,
        Contains = 0x0001,
        In = 0x0002,
        StartsWith = 0x0004,
        EndsWith = 0x0008
    }
}
