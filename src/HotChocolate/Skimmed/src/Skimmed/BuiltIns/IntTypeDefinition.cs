namespace HotChocolate.Skimmed;

public sealed class IntTypeDefinition : ScalarTypeDefinition
{
    internal IntTypeDefinition()
        : base(BuiltIns.Int.Name)
    {
        IsSpecScalar = true;
    }
}
