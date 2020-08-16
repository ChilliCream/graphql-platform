namespace HotChocolate.Types.Filters
{
    public enum FilterOperationKind
        : byte
    {
        Equals = 0x0000,
        NotEquals = 0x0001,

        Contains = 0x0002,
        NotContains = 0x0003,

        In = 0x0004,
        NotIn = 0x0005,

        StartsWith = 0x0006,
        NotStartsWith = 0x0007,

        EndsWith = 0x0008,
        NotEndsWith = 0x0009,

        GreaterThan = 0x0016,
        NotGreaterThan = 0x0017,

        GreaterThanOrEquals = 0x0018,
        NotGreaterThanOrEquals = 0x0019,

        LowerThan = 0x0020,
        NotLowerThan = 0x0021,

        LowerThanOrEquals = 0x0022,
        NotLowerThanOrEquals = 0x0023,

        Object = 0x0024,

        ArraySome = 0x0026,

        ArrayNone = 0x0028,

        ArrayAll = 0x0030,

        ArrayAny = 0x0032,

    }
}
