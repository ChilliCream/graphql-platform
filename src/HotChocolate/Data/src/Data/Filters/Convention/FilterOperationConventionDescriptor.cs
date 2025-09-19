namespace HotChocolate.Data.Filters;

public class FilterOperationConventionDescriptor : IFilterOperationConventionDescriptor
{
    protected FilterOperationConventionDescriptor(int operationId)
    {
        Configuration.Id = operationId;
    }

    protected FilterOperationConventionConfiguration Configuration { get; } = new();

    public FilterOperationConventionConfiguration CreateConfiguration() => Configuration;

    /// <inheritdoc />
    public IFilterOperationConventionDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IFilterOperationConventionDescriptor Description(string description)
    {
        Configuration.Description = description;
        return this;
    }

    public static FilterOperationConventionDescriptor New(int operationId) =>
        new FilterOperationConventionDescriptor(operationId);
}
