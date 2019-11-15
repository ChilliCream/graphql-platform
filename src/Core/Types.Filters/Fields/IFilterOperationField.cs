namespace HotChocolate.Types.Filters
{
    public interface IFilterOperationField
        : IInputField
        , IHasClrType
    {
        FilterOperation Operation { get; }
    }
}
