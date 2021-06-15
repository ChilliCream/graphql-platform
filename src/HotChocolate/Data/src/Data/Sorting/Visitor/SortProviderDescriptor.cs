namespace HotChocolate.Data.Sorting
{
    public class SortProviderDescriptor<TContext>
        : ISortProviderDescriptor<TContext>
        where TContext : ISortVisitorContext
    {
        protected SortProviderDescriptor()
        {
        }

        protected SortProviderDefinition Definition { get; } =
            new SortProviderDefinition();

        public SortProviderDefinition CreateDefinition() => Definition;

        public ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>()
            where TFieldHandler : ISortFieldHandler<TContext>
        {
            Definition.Handlers.Add((typeof(TFieldHandler), null));
            return this;
        }

        public ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
            TFieldHandler fieldHandler)
            where TFieldHandler : ISortFieldHandler<TContext>
        {
            Definition.Handlers.Add((typeof(TFieldHandler), fieldHandler));
            return this;
        }

        public ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>()
            where TOperationHandler : ISortOperationHandler<TContext>
        {
            Definition.OperationHandlers.Add((typeof(TOperationHandler), null));
            return this;
        }

        public ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>(
            TOperationHandler operationHandler)
            where TOperationHandler : ISortOperationHandler<TContext>
        {
            Definition.OperationHandlers.Add((typeof(TOperationHandler), operationHandler));
            return this;
        }
#pragma warning disable CA1000 // Do not declare static members on generic types
        public static SortProviderDescriptor<TContext> New() =>
            new SortProviderDescriptor<TContext>();
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}
