namespace HotChocolate.Configuration
{
    public interface IBindResolver<TResolver>
        : IBoundResolver<TResolver>
        where TResolver : class
    {
        IBoundResolver<TResolver> To(string typeName);

        IBoundResolver<TResolver, TObjectType> To<TObjectType>()
            where TObjectType : class;
    }
}
