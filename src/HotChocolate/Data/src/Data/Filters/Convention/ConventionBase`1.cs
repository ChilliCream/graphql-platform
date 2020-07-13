namespace HotChocolate.Data.Filters
{
    public abstract class ConventionBase<TDefinition>
        where TDefinition : class
    {
        private TDefinition? _definition;

        internal TDefinition? Definition
        {
            get
            {
                return _definition;
            }
        }

        internal void Initialize(IConventionContext context)
        {
            _definition = CreateDefinition(context);
            OnComplete(context, Definition);
        }

        protected abstract TDefinition? CreateDefinition(IConventionContext context);

        protected virtual void OnComplete(IConventionContext context, TDefinition? definition)
        {
        }
    }
}
