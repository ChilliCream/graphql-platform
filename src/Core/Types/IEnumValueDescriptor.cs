namespace HotChocolate.Types
{
    public interface IEnumValueDescriptor
    {
        IEnumValueDescriptor Name(string name);
        IEnumValueDescriptor Description(string description);
        IEnumValueDescriptor DeprecationReason(string deprecationReason);

    }
}
