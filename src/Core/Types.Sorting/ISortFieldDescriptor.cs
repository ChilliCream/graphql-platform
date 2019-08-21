namespace HotChocolate.Types.Sorting
{
    public interface ISortFieldDescriptor
    {
        ISortFieldDescriptor Ignore();
        ISortFieldDescriptor Name(NameString value);
    }
}
