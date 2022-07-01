using System;

namespace HotChocolate.Execution.Processing;

public sealed class ObjectFieldResult : ResultData
{
    private Flags _flags = Flags.Nullable;
    private ResultData? _parent;

    public string Name { get; private set; } = default!;

    public object? Value { get; private set; }

    internal bool IsNullable => (_flags & Flags.Nullable) == Flags.Nullable;

    internal bool IsInitialized => (_flags & Flags.Initialized) == Flags.Initialized;

    internal override ResultData? Parent
    {
        get => _parent;
        set
        {
            if (_parent is not null)
            {
                throw new InvalidOperationException("Parent already set.");
            }
            _parent = value;
        }
    }

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
