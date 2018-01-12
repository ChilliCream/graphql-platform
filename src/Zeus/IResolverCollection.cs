namespace Zeus
{
    public interface IResolverCollection
    {
        bool TryGetResolver(string typeName, string fieldName, out IResolver resolver);
    }


}
