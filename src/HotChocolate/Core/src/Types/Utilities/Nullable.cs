#nullable enable

namespace HotChocolate.Utilities
{
    internal enum Nullable : byte
    {
        Skip = 0,
        Yes = 2,
        No = 1,
        Undefined = 3
    }
}
