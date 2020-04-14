using System;

namespace HotChocolate.Types.Sorting.Conventions
{
    public class SortingExpressionVisitorDescriptor
        : SortingVisitorDescriptorBase<SortingExpressionVisitorDefinition>,
         ISortingExpressionVisitorDescriptor
    {
        private readonly ISortingConventionDescriptor _convention;

        protected SortingExpressionVisitorDescriptor(ISortingConventionDescriptor convention)
        {
            _convention = convention;
        }

        protected override SortingExpressionVisitorDefinition Definition { get; }
            = new SortingExpressionVisitorDefinition();

        public ISortingConventionDescriptor And() => _convention;

        public ISortingExpressionVisitorDescriptor Compile(SortCompiler compiler)
        {
            Definition.Compiler = compiler ??
                throw new ArgumentNullException(nameof(compiler));

            return this;
        }

        public ISortingExpressionVisitorDescriptor CreateOperation(SortOperationFactory factory)
        {
            Definition.OperationFactory = factory ??
                throw new ArgumentNullException(nameof(factory));

            return this;
        }

        public override SortingExpressionVisitorDefinition CreateDefinition()
        {
            return Definition;
        }

        public static SortingExpressionVisitorDescriptor New(
            ISortingConventionDescriptor convention)
                => new SortingExpressionVisitorDescriptor(convention);
    }
}
