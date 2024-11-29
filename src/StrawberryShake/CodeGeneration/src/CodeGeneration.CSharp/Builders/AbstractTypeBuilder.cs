using StrawberryShake.Properties;

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
        if (property is null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        Properties.Add(property);
    }

    public void AddImplements(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException(
                Resources.ClassBuilder_AddImplements_TypeNameCannotBeNull,
                nameof(value));
        }

        Implements.Add(value);
    }
}
