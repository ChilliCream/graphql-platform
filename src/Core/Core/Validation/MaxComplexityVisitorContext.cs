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
        private int _complexity;
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
            Scope = this;
        }

        protected MaxComplexityVisitorContext(
            ImmutableHashSet<string> fragmentPath,
            ImmutableList<IOutputField> fieldPath,
            MaxComplexityVisitorContext context)
        {
            FragmentPath = fragmentPath;
            FieldPath = fieldPath;
            Schema = context.Schema;
            Fragments = context.Fragments;
            TypeContext = context.TypeContext;
            _calculateComplexity = context._calculateComplexity;
            _root = context._root;
            Scope = context.Scope;
        }

        protected MaxComplexityVisitorContext(
            MaxComplexityVisitorContext context)
        {
            Schema = context.Schema;
            FragmentPath = context.FragmentPath;
            FieldPath = context.FieldPath;
            Fragments = context.Fragments;
            TypeContext = context.TypeContext;
            _calculateComplexity = context._calculateComplexity;
            _root = context._root;
            Scope = context.Scope;
        }

        public ISchema Schema { get; }

        public ImmutableHashSet<string> FragmentPath { get; }

        public ImmutableList<IOutputField> FieldPath { get; }

        public INamedOutputType TypeContext { get; protected set; }

        public IDictionary<string, FragmentDefinitionNode> Fragments
        { get; }

        public MaxComplexityVisitorContext Scope { get; protected set; }

        public int Complexity
        {
            get => Scope._complexity;
            protected set => Scope._complexity = value;
        }

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
                this);
        }

        public virtual MaxComplexityVisitorContext AddField(
            IOutputField fieldDefinition,
            FieldNode fieldSelection)
        {
            IDirective directive = fieldDefinition.Directives
                .FirstOrDefault(t => t.Type is CostDirectiveType);

            CostDirective cost = directive == null
                ? DefaultCost
                : directive.ToObject<CostDirective>();

            Complexity = Complexity + CalculateComplexity(
                new ComplexityContext(
                    fieldDefinition, fieldSelection,
                    FieldPath, null, cost));

            if (Complexity > MaxComplexity)
            {
                MaxComplexity = Complexity;
            }

            return new MaxComplexityVisitorContext(
                FragmentPath,
                FieldPath.Add(fieldDefinition),
                this);
        }

        public virtual MaxComplexityVisitorContext SetTypeContext(
            INamedOutputType typeContext)
        {
            var newContext = new MaxComplexityVisitorContext(this);
            newContext.TypeContext = typeContext;
            return newContext;
        }

        public virtual MaxComplexityVisitorContext CreateScope()
        {
            var newContext = new MaxComplexityVisitorContext(this);
            newContext.Scope = newContext;
            return newContext;
        }

        public static MaxComplexityVisitorContext New(
            ISchema schema,
            ComplexityCalculation calculateComplexity) =>
                new MaxComplexityVisitorContext(schema, calculateComplexity);

        public static MaxComplexityVisitorContext New(
            ISchema schema,
            IVariableValueCollection variables,
            ComplexityCalculation calculateComplexity) =>
                new MaxComplexityWithMultipliersVisitorContext(
                    schema, variables, calculateComplexity);
    }
}
