namespace HotChocolate.Fusion.Comparers;

public sealed class SchemaByNameComparer : Comparer<SchemaDefinition>
{
    public override int Compare(SchemaDefinition? x, SchemaDefinition? y)
    {
        return string.CompareOrdinal(x?.Name, y?.Name);
    }
}
