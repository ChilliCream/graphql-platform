using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public interface IEnumTypeDescriptor
        : IFluent
    {
        IEnumTypeDescriptor Name(string name);

        IEnumTypeDescriptor Description(string description);

        IEnumValueDescriptor Item<T>(T value);

        IEnumTypeDescriptor BindItems(BindingBehavior bindingBehavior);
    }

    public interface IEnumTypeDescriptor<T>
        : IEnumTypeDescriptor
    {
        IEnumValueDescriptor Item(T value);

        new IEnumTypeDescriptor<T> BindItems(BindingBehavior bindingBehavior);
    }
}
