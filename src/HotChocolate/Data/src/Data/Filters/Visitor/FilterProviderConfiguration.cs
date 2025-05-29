namespace HotChocolate.Data.Filters;

public class FilterProviderConfiguration
{
    public IList<(Type Handler, IFilterFieldHandler? HandlerInstance)> Handlers { get; } =
        new List<(Type Handler, IFilterFieldHandler? HandlerInstance)>();
}
