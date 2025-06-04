namespace HotChocolate.Data.Filters;

public class FilterProviderConfiguration
{
    public IList<(Type Handler, IFilterFieldHandler? HandlerInstance)> Handlers { get; } = [];
}
