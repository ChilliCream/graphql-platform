#nullable enable
using System;

namespace HotChocolate.Types.Descriptors.Definitions;

[Flags]
internal enum FieldFlags
{
    None = 0,
    Introspection = 2,
    Deprecated = 4,
    Ignored = 8,
    ParallelExecutable = 16,
    Stream = 32,
    Sealed = 64,
    TypeNameField = 128
}
