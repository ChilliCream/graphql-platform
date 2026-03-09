namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public abstract class AbstractTypeBuilder : ITypeBuilder
{
    protected List<PropertyBuilder> Properties { get; } = [];

    protected string? Name { get; private set; }

    protected List<string> Implements { get; } = [];

    public abstract void Build(CodeWriter writer);

    protected void SetName(string name)
    {
        Name = name;
    }

    public void AddProperty(PropertyBuilder property)
    {
        ArgumentNullException.ThrowIfNull(property);

        Properties.Add(property);
    }

    public void AddImplements(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        Implements.Add(value);
    }
}
