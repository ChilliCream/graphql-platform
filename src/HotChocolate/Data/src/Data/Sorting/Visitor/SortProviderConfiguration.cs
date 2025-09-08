namespace HotChocolate.Data.Sorting;

public class SortProviderConfiguration
{
    public IList<(Type Handler, ISortFieldHandler? HandlerInstance)> Handlers { get; } = [];

    public IList<(Type Handler, ISortOperationHandler? HandlerInstance)> OperationHandlers { get; }
        = [];
}
