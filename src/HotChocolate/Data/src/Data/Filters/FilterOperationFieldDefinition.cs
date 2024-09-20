namespace HotChocolate.Data.Filters;

public class FilterOperationFieldDefinition : FilterFieldDefinition
{
    public int Id { get; set; }

    internal void CopyTo(FilterOperationFieldDefinition target)
    {
        base.CopyTo(target);
        target.Id = Id;
    }

    internal void MergeInto(FilterOperationFieldDefinition target)
    {
        base.MergeInto(target);
        target.Id = Id;
    }
}
