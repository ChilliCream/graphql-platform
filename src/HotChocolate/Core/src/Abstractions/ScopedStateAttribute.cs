namespace HotChocolate;

[AttributeUsage(AttributeTargets.Parameter)]
public class ScopedStateAttribute : Attribute
{
    public ScopedStateAttribute()
    {
    }

    public ScopedStateAttribute(string key)
    {
        Key = key;
    }

    public string? Key { get; }
}
