namespace HotChocolate.Data.Filters;

public interface IFilterFieldInfo : IFilterMemberInfo
{
    IFilterField Field { get; }
}

public interface IFilterMemberInfo
{
    IFilterValue? Value { get; }
}
