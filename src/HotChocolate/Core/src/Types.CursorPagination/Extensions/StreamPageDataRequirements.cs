namespace HotChocolate.Types.Pagination;

[Flags]
internal enum StreamPageDataRequirements
{
    None = 0,
    TotalCount = 1,
    FirstRow = 2,
    AllRows = 4
}
