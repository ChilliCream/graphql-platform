namespace HotChocolate.Validation;

public record ComplexInput3(string Name, string Owner, List<ComplexInput3>? ChildList = null);
