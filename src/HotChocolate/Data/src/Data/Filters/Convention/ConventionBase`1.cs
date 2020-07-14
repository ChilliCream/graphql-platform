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

        public string? Scope { get; set; }

        internal void Initialize(IConventionContext context)
        {
            Scope = context.Scope;
            _definition = CreateDefinition(context);
            OnComplete(context, Definition);
        }

        protected abstract TDefinition? CreateDefinition(IConventionContext context);

        protected virtual void OnComplete(IConventionContext context, TDefinition? definition)
        {
        }
    }
}
