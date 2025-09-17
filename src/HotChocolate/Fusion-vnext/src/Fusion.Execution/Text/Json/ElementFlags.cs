namespace HotChocolate.Fusion.Text.Json;

[Flags]
internal enum ElementFlags : byte
{
    None = 0,

    // 0x01 - For error propagation
    Invalidated = 1,

    // 0x02 - Data stored in composite (not source document)
    Local = 2,

    // 0x04 - Field can be null (schema info)
    IsNullable = 4,

    // 0x08 - Element has no parent (ignore ParentRow value)
    IsRoot = 8,

    // 0x10 - Element is internal and mustnt be written to the output stream.
    IsInternal = 16,

    // 0x20 - Element is a leaf node
    IsLeaf = 32
}
