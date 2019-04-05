namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptorContext
    {
        INamingConventions Naming { get; }

        ITypeInspector Inspector { get; }
    }
}
