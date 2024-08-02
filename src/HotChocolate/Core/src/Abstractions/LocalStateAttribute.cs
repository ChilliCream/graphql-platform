namespace HotChocolate;

[AttributeUsage(AttributeTargets.Parameter)]
public class LocalStateAttribute : Attribute
{
    public LocalStateAttribute()
    {
    }

    public LocalStateAttribute(string key)
    {
        Key = key;
    }

    public string? Key { get; }
}
