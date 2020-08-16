namespace HotChocolate.Types.Filters
{
    public interface IFilterOperationField
        : IInputField
        , IHasRuntimeType
    {
        FilterOperation Operation { get; }
    }
}
