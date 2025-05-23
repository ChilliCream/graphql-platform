namespace HotChocolate.Data.Projections;

[Flags]
public enum SelectionFlags
{
    None = 0,
    FirstOrDefault = 1,
    SingleOrDefault = 2,
    MemberIsList = 4,
}
