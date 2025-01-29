namespace GreenDonut.Data;

internal readonly struct QueryState
{
    public QueryState(string key, object value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }

    public object Value { get; }
}
