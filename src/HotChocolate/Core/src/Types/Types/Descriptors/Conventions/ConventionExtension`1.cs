namespace HotChocolate.Types.Descriptors
{
    public abstract class ConventionExtension<TDefinition>
        : Convention<TDefinition>,
          IConventionExtension
        where TDefinition : class
    {
        public abstract void Merge(IConventionContext context, Convention convention);
    }
}
