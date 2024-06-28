namespace HotChocolate.Skimmed;

public sealed class FloatTypeDefinition : ScalarTypeDefinition
{
    internal FloatTypeDefinition()
        : base(BuiltIns.Float.Name)
    {
        IsSpecScalar = true;
    }
}
