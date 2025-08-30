namespace HotChocolate.Execution;

internal sealed class SchemaName : IEquatable<SchemaName>
{
    public SchemaName(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;

    public bool Equals(SchemaName? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Value == other.Value;
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj)
            || (obj is SchemaName other && Equals(other));

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
