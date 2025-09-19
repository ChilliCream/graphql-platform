namespace HotChocolate.Fusion.Comparers;

public sealed class SchemaByNameComparer<T> : Comparer<T> where T : ISchemaDefinition
{
    public override int Compare(T? x, T? y)
    {
        return string.CompareOrdinal(x?.Name, y?.Name);
    }
}
