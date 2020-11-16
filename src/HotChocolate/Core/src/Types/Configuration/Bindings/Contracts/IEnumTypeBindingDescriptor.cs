namespace HotChocolate.Configuration.Bindings
{
    public interface IEnumTypeBindingDescriptor : IFluent
    {
        IEnumTypeBindingDescriptor To(NameString typeName);

        IEnumValueBindingDescriptor Value(object value);
    }
}
