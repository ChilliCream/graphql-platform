using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class MaxDepthVisitor
        : QuerySyntaxWalker<MaxDepthVisitor.Context>
    {
        private readonly int _maxExecutionDepth;

        public MaxDepthVisitor(IValidateQueryOptionsAccessor options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _maxExecutionDepth = options.MaxExecutionDepth ?? int.MaxValue;
        }

        protected override bool VisitFragmentDefinitions => false;

        public IReadOnlyCollection<FieldNode> Visit(DocumentNode node)
        {
            var context = Context.New();
            Visit(node, context);
            return context.ViolatingFields;
        }

        protected override void VisitDocument(
            DocumentNode node,
            Context context)
        {
            foreach (FragmentDefinitionNode fragment in node.Definitions
                .OfType<FragmentDefinitionNode>()
                .Where(t => t.Name?.Value != null))
            {
                context.Fragments[fragment.Name.Value] = fragment;
            }

            foreach (OperationDefinitionNode operation in node.Definitions
                .OfType<OperationDefinitionNode>())
            {
                VisitOperationDefinition(operation, context);
            }
        }

        protected override void VisitField(
            FieldNode node,
            Context context)
        {
            Context current = context.AddField(node);

            if (current.FieldPath.Count > _maxExecutionDepth)
            {
                current.AddViolation(node);
            }

            base.VisitField(node, current);
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode node,
            Context context)
        {
            base.VisitFragmentSpread(node, context);

            if (context.Fragments.TryGetValue(node.Name.Value,
                out FragmentDefinitionNode fragment))
            {
                VisitFragmentDefinition(fragment, context);
            }
        }

        protected override void VisitFieldDefinition(
            FieldDefinitionNode node,
            Context context)
        {
            if (!context.FragmentPath.Contains(node.Name.Value))
            {
                base.VisitFieldDefinition(node, context);
            }
        }

        internal sealed class Context
        {
            private readonly List<FieldNode> _violatingFields;

            private Context()
            {
                _violatingFields = new List<FieldNode>();
                FragmentPath = ImmutableHashSet<string>.Empty;
                FieldPath = ImmutableList<FieldNode>.Empty;
                Fragments = new Dictionary<string, FragmentDefinitionNode>();
            }

            private Context(
                ImmutableHashSet<string> fragmentPath,
                ImmutableList<FieldNode> fieldPath,
                List<FieldNode> violatingFields,
                IDictionary<string, FragmentDefinitionNode> fragments)
            {
                FragmentPath = fragmentPath;
                FieldPath = fieldPath;
                _violatingFields = violatingFields;
                Fragments = fragments;
            }

            public ImmutableHashSet<string> FragmentPath { get; }

            public ImmutableList<FieldNode> FieldPath { get; }

            public IDictionary<string, FragmentDefinitionNode> Fragments
            { get; }

            public IReadOnlyCollection<FieldNode> ViolatingFields =>
                _violatingFields;

            public Context AddFragment(FragmentDefinitionNode fragment)
            {
                return new Context(
                    FragmentPath.Add(fragment.Name.Value),
                    FieldPath,
                    _violatingFields,
                    Fragments);
            }

            public Context AddField(FieldNode field)
            {
                return new Context(
                    FragmentPath,
                    FieldPath.Add(field),
                    _violatingFields,
                    Fragments);
            }

            public void AddViolation(FieldNode field)
            {
                _violatingFields.Add(field);
            }

            public static Context New() => new Context();
        }
    }
}
