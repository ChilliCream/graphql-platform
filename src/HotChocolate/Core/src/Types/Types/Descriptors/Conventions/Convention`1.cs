#nullable enable
namespace HotChocolate.Types.Descriptors
{
    public abstract class Convention<TDefinition> : Convention
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

        public override void Initialize(IConventionContext context)
        {
            Scope = context.Scope;
            _definition = CreateDefinition(context);
            OnComplete(context, Definition);
            MarkInitialized();
        }

        protected virtual void OnComplete(IConventionContext context, TDefinition? definition)
        {
        }

        protected abstract TDefinition? CreateDefinition(IConventionContext context);
    }
}
