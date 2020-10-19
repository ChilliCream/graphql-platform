namespace HotChocolate.Data
{
    // TODO: Kill this interface!
    public interface IModelId<TKey> where TKey : class
    {
        TKey Id { get; set; }
    }
}
