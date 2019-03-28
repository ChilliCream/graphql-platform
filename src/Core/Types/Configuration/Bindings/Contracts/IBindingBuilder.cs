namespace HotChocolate.Configuration.Bindings
{
    public interface IBindingBuilder
    {
        IBindingInfo Create();
        bool IsComplete();
    }

}
