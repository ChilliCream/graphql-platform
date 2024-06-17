namespace HotChocolate.Skimmed;

public sealed class BooleanTypeDefinition : ScalarTypeDefinition
{
    internal BooleanTypeDefinition()
        : base(BuiltIns.Boolean.Name)
    {
        IsSpecScalar = true;
    }
}
