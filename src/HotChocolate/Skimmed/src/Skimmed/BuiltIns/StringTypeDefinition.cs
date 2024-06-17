namespace HotChocolate.Skimmed;

public sealed class StringTypeDefinition : ScalarTypeDefinition
{
    internal StringTypeDefinition()
        : base(BuiltIns.String.Name)
    {
        IsSpecScalar = true;
    }
}
