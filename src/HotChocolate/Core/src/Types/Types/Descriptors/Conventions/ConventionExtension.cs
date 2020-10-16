namespace HotChocolate.Types.Descriptors
{
    public abstract class ConventionExtension
        : Convention,
          IConventionExtension
    {
        public abstract void Merge(IConventionContext context, Convention convention);
    }
}
