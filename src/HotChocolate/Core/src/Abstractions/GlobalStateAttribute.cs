namespace HotChocolate;

[AttributeUsage(AttributeTargets.Parameter)]
public class GlobalStateAttribute : Attribute
{
    public GlobalStateAttribute()
    {
    }

    public GlobalStateAttribute(string key)
    {
        Key = key;
    }

    public string? Key { get; }
}
