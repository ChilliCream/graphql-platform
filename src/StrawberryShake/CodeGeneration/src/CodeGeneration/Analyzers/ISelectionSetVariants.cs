namespace StrawberryShake.CodeGeneration.Analyzers;

public class SelectionSetVariants
{
    public SelectionSetVariants(
        SelectionSet returnType,
        IReadOnlyList<SelectionSet>? variants = null)
    {
        ReturnType = returnType;
        Variants = variants ?? new [] { returnType, };
    }

    public SelectionSet ReturnType { get; }

    public IReadOnlyList<SelectionSet> Variants { get; }
}
