namespace HotChocolate.Configuration.Bindings
{
    public interface IBindingInfo
    {
        bool IsValid();

        IBindingInfo Clone();
    }
}
