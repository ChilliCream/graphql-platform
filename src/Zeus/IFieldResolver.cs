namespace Zeus
{
    public interface IFieldResolver
        : IResolver
    {
        string TypeName { get; }
        string FieldName { get; }
    }

    public interface IFieldResolver<TResult>
         : IFieldResolver
         , IResolver<TResult>
    {
    }
}
