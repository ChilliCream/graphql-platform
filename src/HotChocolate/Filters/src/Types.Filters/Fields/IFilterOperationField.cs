namespace HotChocolate.Types.Filters
{
    public interface IFilterOperationField
        : IInputField
    {
        FilterOperation Operation { get; }
    }
}
