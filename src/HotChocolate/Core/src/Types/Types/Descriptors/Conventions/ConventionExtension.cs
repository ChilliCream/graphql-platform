namespace HotChocolate.Types.Descriptors
{
    public abstract class ConventionExtension
        : Convention
        , IConventionExtension
    {
        public abstract void Merge(IConventionContext context, Convention convention);

        protected internal sealed override void OnComplete(IConventionContext context)
        {
            base.OnComplete(context);
        }
    }
}
