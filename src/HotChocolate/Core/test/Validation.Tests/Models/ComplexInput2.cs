namespace HotChocolate.Validation;

public class ComplexInput2(string name, string owner, ComplexInput2? child = null)
{
    public string Name { get; set; } = name;

    public string Owner { get; set; } = owner;

    public ComplexInput2? Child { get; set; } = child;
}
