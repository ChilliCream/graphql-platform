using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{

    internal sealed class MaxDepthVisitor
        : QuerySyntaxWalker<MaxDepthVisitor.Context>
    {
        private readonly int _maxExecutionDepth;
        private readonly Dictionary<string, FragmentDefinitionNode> _fragments =
            new Dictionary<string, FragmentDefinitionNode>();
        private readonly List<FieldNode> _violatingFields =
            new List<FieldNode>();

        public MaxDepthVisitor(IValidateQueryOptionsAccessor options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!options.MaxExecutionDepth.HasValue)
            {
                throw new ArgumentException(
                    "The max depth visitor can only be used " +
                    "if a max query depth is defined.");
            }

            _maxExecutionDepth = options.MaxExecutionDepth.Value;
        }

        protected override bool VisitFragmentDefinitions => false;

        internal bool IsMaxDepthReached { get; private set; }

        internal IReadOnlyCollection<FieldNode> ViolatingFields { get; }

        public void Visit(DocumentNode node)
        {
            Visit(node, Context.New());
        }

        protected override void VisitDocument(
            DocumentNode node,
            MaxDepthVisitor.Context context)
        {
            foreach (var fragment in node.Definitions
                .OfType<FragmentDefinitionNode>())
            {
                _fragments[fragment.Name.Value] = fragment;
            }

            foreach (var operation in node.Definitions
                .OfType<OperationDefinitionNode>())
            {
                VisitOperationDefinition(operation, context);
            }
        }

        protected override void VisitField(
            FieldNode field,
            MaxDepthVisitor.Context context)
        {
            MaxDepthVisitor.Context current = context.AddField(field);
            if (current.FieldPath.Count > _maxExecutionDepth)
            {
                IsMaxDepthReached = true;
                _violatingFields.Add(field);
            }

            base.VisitField(field, context);
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode node,
            MaxDepthVisitor.Context context)
        {
            base.VisitFragmentSpread(node, context);

            if (_fragments.TryGetValue(node.Name.Value,
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
            private Context()
            {
                FragmentPath = ImmutableHashSet<string>.Empty;
                FieldPath = ImmutableList<FieldNode>.Empty;
            }

            private Context(
                ImmutableHashSet<string> fragmentPath,
                ImmutableList<FieldNode> fieldPath)
            {
                FragmentPath = fragmentPath;
                FieldPath = fieldPath;
            }

            public ImmutableHashSet<string> FragmentPath { get; }

            public ImmutableList<FieldNode> FieldPath { get; }

            public Context AddFragment(FragmentDefinitionNode fragment)
            {
                return new Context(
                    FragmentPath.Add(fragment.Name.Value),
                    FieldPath);
            }

            public Context AddField(FieldNode field)
            {
                return new Context(
                    FragmentPath,
                    FieldPath.Add(field));
            }

            public static Context New() => new Context();
        }
    }
}
