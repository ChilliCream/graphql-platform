namespace HotChocolate.Validation;

public class ComplexInput
{
    public string? Name { get; set; }

    public string? Owner { get; set; }

    public ComplexInput? Child { get; set; }
}
