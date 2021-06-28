namespace HotChocolate.Configuration.Bindings
{
    public interface IEnumValueBindingDescriptor : IFluent
    {
        IEnumTypeBindingDescriptor To(NameString valueName);
    }
}
