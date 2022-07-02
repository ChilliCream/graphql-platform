using System;

namespace HotChocolate.Execution.Processing;

public sealed class ObjectFieldResult
{
    private Flags _flags = Flags.Nullable;

    public string Name { get; private set; } = default!;

    public object? Value { get; private set; }

    internal bool IsNullable => (_flags & Flags.Nullable) == Flags.Nullable;

    internal bool IsInitialized => (_flags & Flags.Initialized) == Flags.Initialized;

    internal void Set(string name, object? value, bool isNullable)
    {
        Name = name;
        Value = value;

        if (isNullable)
        {
            _flags = Flags.Nullable | Flags.Initialized;
        }
        else
        {
            _flags = Flags.Initialized;
        }
    }

    internal void Reset()
    {
        Name = default!;
        Value = null;
        _flags = Flags.Nullable;
    }

    [Flags]
    private enum Flags : byte
    {
        Initialized = 1,
        Nullable = 2
    }
}
