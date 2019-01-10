using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal class MaxComplexityVisitorContext
    {
        protected static readonly CostDirective DefaultCost =
            new CostDirective();
        private readonly MaxComplexityVisitorContext _root;
        private readonly ComplexityCalculation _calculateComplexity;
        private readonly int _complexity;
        private int _maxComplexity;

        protected MaxComplexityVisitorContext(
            ISchema schema,
            ComplexityCalculation calculateComplexity)
        {
            _calculateComplexity = calculateComplexity;
            Schema = schema;
            FragmentPath = ImmutableHashSet<string>.Empty;
            FieldPath = ImmutableList<IOutputField>.Empty;
            Fragments = new Dictionary<string, FragmentDefinitionNode>();
            _root = this;
        }

        protected MaxComplexityVisitorContext(
            ImmutableHashSet<string> fragmentPath,
            ImmutableList<IOutputField> fieldPath,
            int complexity,
            MaxComplexityVisitorContext context)
        {
            FragmentPath = fragmentPath;
            FieldPath = fieldPath;
            Schema = context.Schema;
            Fragments = context.Fragments;
            TypeContext = context.TypeContext;
            _complexity = complexity;
            _calculateComplexity = context._calculateComplexity;
            _root = context._root;
        }

        protected MaxComplexityVisitorContext(
            MaxComplexityVisitorContext context)
        {
            Schema = context.Schema;
            FragmentPath = context.FragmentPath;
            FieldPath = context.FieldPath;
            Fragments = context.Fragments;
            TypeContext = context.TypeContext;
            _complexity = context._complexity;
            _calculateComplexity = context._calculateComplexity;
            _root = context._root;
        }

        public ISchema Schema { get; }

        public ImmutableHashSet<string> FragmentPath { get; }

        public ImmutableList<IOutputField> FieldPath { get; }

        public INamedOutputType TypeContext { get; protected set; }

        public IDictionary<string, FragmentDefinitionNode> Fragments
        { get; }

        public int Complexity { get; protected set; }

        public int MaxComplexity
        {
            get
            {
                return _root._maxComplexity;
            }
            protected set
            {
                _root._maxComplexity = value;
            }
        }

        public ComplexityCalculation CalculateComplexity =>
            _calculateComplexity;

        public virtual MaxComplexityVisitorContext AddFragment(
            FragmentDefinitionNode fragment)
        {
            return new MaxComplexityVisitorContext(
                FragmentPath.Add(fragment.Name.Value),
                FieldPath,
                _complexity,
                this);
        }

        public virtual MaxComplexityVisitorContext AddField(
            IOutputField fieldDefinition,
            FieldNode fieldSelection)
        {
            IDirective directive = fieldDefinition.Directives
                .FirstOrDefault(t => t.Type is CostDirectiveType);
            int complexity;

            CostDirective cost = directive == null
                ? DefaultCost
                : directive.ToObject<CostDirective>();

            complexity = _complexity + CalculateComplexity(
                new ComplexityContext(
                    fieldDefinition, fieldSelection,
                    FieldPath, null, cost));

            if (complexity > MaxComplexity)
            {
                MaxComplexity = complexity;
            }

            return new MaxComplexityVisitorContext(
                FragmentPath,
                FieldPath.Add(fieldDefinition),
                complexity,
                this);
        }

        public virtual MaxComplexityVisitorContext SetTypeContext(
            INamedOutputType typeContext)
        {
            var newContext = new MaxComplexityVisitorContext(this);
            newContext.TypeContext = typeContext;
            return newContext;
        }

        public static MaxComplexityVisitorContext New(
            ISchema schema,
            ComplexityCalculation calculateComplexity) =>
                new MaxComplexityVisitorContext(schema, calculateComplexity);

        public static MaxComplexityVisitorContext New(
            ISchema schema,
            IVariableCollection variables,
            ComplexityCalculation calculateComplexity) =>
                new MaxComplexityWithMultipliersVisitorContext(
                    schema, variables, calculateComplexity);
    }
}
