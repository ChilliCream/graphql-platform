namespace HotChocolate.Data.Sorting;

public class SortOperationConventionDescriptor : ISortOperationConventionDescriptor
{
    protected SortOperationConventionDescriptor(int operationId)
    {
        Definition.Id = operationId;
    }

    protected SortOperationConventionDefinition Definition { get; } = new();

    public SortOperationConventionDefinition CreateConfiguration() => Definition;

    /// <inheritdoc />
    public ISortOperationConventionDescriptor Name(string name)
    {
        Definition.Name = name;
        return this;
    }

    /// <inheritdoc />
    public ISortOperationConventionDescriptor Description(string description)
    {
        Definition.Description = description;
        return this;
    }

    public static SortOperationConventionDescriptor New(int operationId) =>
        new SortOperationConventionDescriptor(operationId);
}
