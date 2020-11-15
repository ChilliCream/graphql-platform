namespace HotChocolate.Types.Descriptors
{
    public abstract class ConventionExtension
        : Convention
        , IConventionExtension
    {
        public abstract void Merge(IConventionContext context, Convention convention);

        protected internal sealed override void Complete(IConventionContext context)
        {
            base.Complete(context);
        }
    }
}
