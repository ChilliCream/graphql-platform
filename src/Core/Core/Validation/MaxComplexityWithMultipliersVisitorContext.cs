using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal class MaxComplexityWithMultipliersVisitorContext
        : MaxComplexityVisitorContext
    {
        private IVariableCollection _variables;

        internal protected MaxComplexityWithMultipliersVisitorContext(
            ISchema schema,
            IVariableCollection variables,
            ComplexityCalculation calculateComplexity)
            : base(schema, calculateComplexity)
        {
            _variables = variables;
        }

        protected MaxComplexityWithMultipliersVisitorContext(
            MaxComplexityWithMultipliersVisitorContext context)
            : base(context)
        {
            _variables = context._variables;
        }

        protected MaxComplexityWithMultipliersVisitorContext(
            ImmutableHashSet<string> fragmentPath,
            ImmutableList<IOutputField> fieldPath,
            MaxComplexityWithMultipliersVisitorContext context)
            : base(fragmentPath, fieldPath, context)
        {
            _variables = context._variables;
        }

        public override MaxComplexityVisitorContext AddFragment(
           FragmentDefinitionNode fragment)
        {
            return new MaxComplexityWithMultipliersVisitorContext(
                FragmentPath.Add(fragment.Name.Value),
                FieldPath,
                this);
        }

        public override MaxComplexityVisitorContext AddField(
            IOutputField fieldDefinition,
            FieldNode fieldSelection)
        {
            IDirective directive = fieldDefinition.Directives
                .FirstOrDefault(t => t.Type is CostDirectiveType);
            int complexity;

            CostDirective cost = directive == null
                ? DefaultCost
                : directive.ToObject<CostDirective>();

            complexity = Complexity + CalculateComplexity(
                new ComplexityContext(
                    fieldDefinition, fieldSelection,
                    FieldPath, _variables, cost));

            if (complexity > MaxComplexity)
            {
                MaxComplexity = complexity;
            }

            return new MaxComplexityWithMultipliersVisitorContext(
                FragmentPath,
                FieldPath.Add(fieldDefinition),
                this);
        }

        public override MaxComplexityVisitorContext SetTypeContext(
            INamedOutputType typeContext)
        {
            var newContext =
                new MaxComplexityWithMultipliersVisitorContext(this);
            newContext.TypeContext = typeContext;
            return newContext;
        }

        public override MaxComplexityVisitorContext CreateScope()
        {
            var newContext =
                new MaxComplexityWithMultipliersVisitorContext(this);
            newContext.Scope = newContext;
            return newContext;
        }
    }
}
