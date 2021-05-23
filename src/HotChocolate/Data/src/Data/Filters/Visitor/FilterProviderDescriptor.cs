namespace HotChocolate.Data.Filters
{
    public class FilterProviderDescriptor<TContext>
        : IFilterProviderDescriptor<TContext>
        where TContext : IFilterVisitorContext
    {
        protected FilterProviderDescriptor()
        {
        }

        protected FilterProviderDefinition Definition { get; } =
            new FilterProviderDefinition();

        public FilterProviderDefinition CreateDefinition() => Definition;

        public IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>()
            where TFieldHandler : IFilterFieldHandler<TContext>
        {
            Definition.Handlers.Add((typeof(TFieldHandler), null));
            return this;
        }

        public IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
            TFieldHandler fieldHandler)
            where TFieldHandler : IFilterFieldHandler<TContext>
        {
            Definition.Handlers.Add((typeof(TFieldHandler), fieldHandler));
            return this;
        }

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static FilterProviderDescriptor<TContext> New() =>
            new FilterProviderDescriptor<TContext>();
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}
