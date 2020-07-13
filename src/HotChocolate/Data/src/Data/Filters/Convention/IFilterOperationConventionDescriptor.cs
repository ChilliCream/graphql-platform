namespace HotChocolate.Data.Filters
{
    public interface IFilterOperationConventionDescriptor
        : IFluent
    {
        IFilterOperationConventionDescriptor Name(string name);

        IFilterOperationConventionDescriptor Description(string description);
    }
}
