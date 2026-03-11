namespace HotChocolate.Data.Sorting;

public class SortProviderConfiguration
{
    public IList<SortFieldHandlerConfiguration> FieldHandlerConfigurations { get; } = [];

    public IList<SortOperationHandlerConfiguration> OperationHandlerConfigurations { get; } = [];
}
