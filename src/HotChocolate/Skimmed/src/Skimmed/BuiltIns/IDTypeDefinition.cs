namespace HotChocolate.Skimmed;

public sealed class IDTypeDefinition : ScalarTypeDefinition
{
    internal IDTypeDefinition()
        : base(BuiltIns.ID.Name)
    {
        IsSpecScalar = true;
    }
}
