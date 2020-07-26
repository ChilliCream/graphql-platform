namespace HotChocolate.Data.Filters
{
    public abstract class FilterProviderBase<TDefinition> : FilterProviderBase
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

        public override void Initialize(IFilterProviderInitializationContext context)
        {
            Scope = context.Scope;
            Convention = context.Convention;
            _definition = CreateDefinition(context);
            OnComplete(context, Definition);
            MarkInitialized();
        }

        protected virtual void OnComplete(
            IFilterProviderInitializationContext context,
            TDefinition? definition)
        {
        }

        protected abstract TDefinition? CreateDefinition(
            IFilterProviderInitializationContext context);
    }
}

