namespace HotChocolate.Fusion;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OfficialV1SuiteAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}
