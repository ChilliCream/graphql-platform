namespace Zeus
{
    public interface IFieldResolver
        : IResolver
    {
        string TypeName { get; }
        string FieldName { get; }
    }


}
