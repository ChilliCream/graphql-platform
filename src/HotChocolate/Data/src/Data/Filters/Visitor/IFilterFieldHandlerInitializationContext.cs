namespace HotChocolate.Data.Filters
{
    public interface IFilterFieldHandlerInitializationContext
        : IFilterProviderInitializationContext
    {
        IFilterProvider Provider { get; }
    }
}
