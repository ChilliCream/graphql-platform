namespace HotChocolate.Fusion.Comparers;

public sealed class SchemaByNameComparer : Comparer<ISchemaDefinition>
{
    public override int Compare(ISchemaDefinition? x, ISchemaDefinition? y)
    {
        return string.CompareOrdinal(x?.Name, y?.Name);
    }
}
