using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal static class FragmentIndexer
{
    public static IReadOnlyDictionary<string, FragmentIndexEntry> Index(
        ISchema schema,
        DocumentNode document)
    {
        var context = new FragmentIndexerContext(schema);
        CollectFragments(context, document);
        FragmentDependencyAnalyzer.Run(context);
        return context.Fragments;
    }

    private static void CollectFragments(
        FragmentIndexerContext context,
        DocumentNode document)
    {
        foreach (IDefinitionNode definition in document.Definitions)
        {
            if (definition.Kind is SyntaxKind.FragmentDefinition)
            {
                var fragment = (FragmentDefinitionNode)definition;
                var fragmentName = fragment.Name.Value;

                if (context.Fragments.ContainsKey(fragmentName))
                {
                    throw ThrowHelper.FragmentNotUnique(fragmentName);
                }

                if (!context.Schema.TryGetTypeFromAst<INamedOutputType>(
                    fragment.TypeCondition,
                    out INamedOutputType? typeCondition))
                {
                    throw ThrowHelper.FragmentInvalidTypeCondition(fragmentName);
                }

                context.Fragments.Add(fragmentName, new(fragment, typeCondition));
            }
        }
    }

    private sealed class FragmentDependencyAnalyzer : SyntaxWalker<FragmentIndexerContext>
    {
        protected override ISyntaxVisitorAction VisitChildren(
            FragmentSpreadNode node,
            FragmentIndexerContext context)
        {
            var fragmentName = node.Name.Value;

            if (!context.Fragments.TryGetValue(fragmentName, out FragmentIndexEntry? index))
            {
                throw ThrowHelper.FragmentNotFound(fragmentName);
            }

            // we register the discovered dependency to another fragment.
            context.Root.DependsOn.Add(fragmentName);

            if (context.Level is 0)
            {
                // if we find a directive dependency that is on the same level,
                // we will register it as sibling.
                context.Root.Siblings.Add(fragmentName);
            }

            ISyntaxVisitorAction action = Continue;

            // we ensure that we do not run in circles here ;)
            if (context.Visited.Add(fragmentName))
            {
                action = VisitChildren(index.Fragment, context);
                context.Visited.Remove(fragmentName);
            }

            return action;
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            FragmentIndexerContext context)
        {
            context.Level++;
            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode node,
            FragmentIndexerContext context)
        {
            context.Level--;
            return base.Enter(node, context);
        }

        public static void Run(FragmentIndexerContext context)
        {
            var analyzer = new FragmentDependencyAnalyzer();

            foreach (FragmentIndexEntry index in context.Fragments.Values)
            {
                context.Root = index;
                context.Level = 0;
                context.Visited.Clear();
                analyzer.Visit(index.Fragment, context);
            }
        }
    }

    private sealed class FragmentIndexerContext : ISyntaxVisitorContext
    {
        public FragmentIndexerContext(ISchema schema)
        {
            Schema = schema;
        }

        public ISchema Schema { get; }

        public FragmentIndexEntry Root { get; set; } = default!;

        public int Level { get; set; }

        public Dictionary<string, FragmentIndexEntry> Fragments { get; } = new();

        public HashSet<string> Visited { get; } = new();
    }
}
