namespace HotChocolate.Execution.Processing;

public sealed class ObjectFieldResult
{
    private Flags _flags = Flags.Nullable;
    private string _name = default!;
    private object? _value;

    public string Name => _name;

    public object? Value => _value;

    internal bool IsNullable => (_flags & Flags.Nullable) == Flags.Nullable;

    internal bool IsInitialized => (_flags & Flags.Initialized) == Flags.Initialized;

    internal void Set(string name, object? value, bool isNullable)
    {
        _name = name;
        _value = value;
        _flags = isNullable ? Flags.InitializedAndNullable : Flags.Initialized;
    }

    internal bool TrySetNull()
    {
        _value = null;

        if ((_flags & Flags.InitializedAndNullable) == Flags.InitializedAndNullable)
        {
            return true;
        }

        return false;
    }

    internal void Reset()
    {
        _name = default!;
        _value = null;
        _flags = Flags.Nullable;
    }

    [Flags]
    private enum Flags : byte
    {
        Initialized = 1,
        Nullable = 2,
        InitializedAndNullable = Initialized | Nullable,
    }
}
