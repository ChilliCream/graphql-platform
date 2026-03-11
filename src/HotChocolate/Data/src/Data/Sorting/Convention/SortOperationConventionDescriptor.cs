namespace HotChocolate.Data.Sorting;

public class SortOperationConventionDescriptor : ISortOperationConventionDescriptor
{
    protected SortOperationConventionDescriptor(int operationId)
    {
        Configuration.Id = operationId;
    }

    protected SortOperationConventionConfiguration Configuration { get; } = new();

    public SortOperationConventionConfiguration CreateConfiguration() => Configuration;

    /// <inheritdoc />
    public ISortOperationConventionDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public ISortOperationConventionDescriptor Description(string description)
    {
        Configuration.Description = description;
        return this;
    }

    public static SortOperationConventionDescriptor New(int operationId) =>
        new SortOperationConventionDescriptor(operationId);
}
