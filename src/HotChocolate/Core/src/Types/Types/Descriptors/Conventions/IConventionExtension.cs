namespace HotChocolate.Types.Descriptors
{
    public interface IConventionExtension : IConvention
    {
        void Merge(IConventionContext context, Convention convention);
    }
}
