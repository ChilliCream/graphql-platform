namespace StrawberryShake.CodeGeneration.Analyzers;

public class SelectionSetVariants(
    SelectionSet returnType,
    IReadOnlyList<SelectionSet>? variants = null)
{
    public SelectionSet ReturnType { get; } = returnType;

    public IReadOnlyList<SelectionSet> Variants { get; } = variants ?? [returnType];
}
