namespace Zeus
{
    public interface IFieldResolver<TResult>
        : IFieldResolver
        , IResolver<TResult>
    {
    }


}
