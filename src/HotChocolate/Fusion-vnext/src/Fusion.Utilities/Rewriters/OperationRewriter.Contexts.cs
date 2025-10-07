using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed partial class OperationRewriter
{
    private abstract class BaseContext(
        BaseContext? parent,
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
    {
        public BaseContext? Parent { get; } = parent;

        public ITypeDefinition Type { get; } = type;

        public Dictionary<Conditional, ConditionalContext>? ConditionalContexts { get; set; }

        public Dictionary<FieldNode, Context>? FieldContexts { get; set; }

        public Dictionary<InlineFragmentNode, Context>? FragmentContexts { get; set; }

        public HashSet<ISelectionNode>? Selections { get; set; }

        public FragmentDefinitionNode GetFragmentDefinition(string name)
            => fragmentLookup[name];

        public virtual BaseContext GetOrCreateFragmentContext(
            InlineFragmentNode inlineFragmentNode,
            ITypeDefinition typeCondition)
        {
            FragmentContexts ??= new Dictionary<InlineFragmentNode, Context>(InlineFragmentNodeComparer.Instance);

            if (!FragmentContexts.TryGetValue(inlineFragmentNode, out var fragmentContext))
            {
                fragmentContext = new Context(this, typeCondition, fragmentLookup);
                FragmentContexts[inlineFragmentNode] = fragmentContext;

                AddSelection(inlineFragmentNode);
            }

            return fragmentContext;
        }

        public ConditionalContext RecreateConditionalContextHierarchy(BaseContext newRoot, ConditionalContext baseConditional)
        {
            var conditionalParents = new Stack<ConditionalContext>();
            BaseContext? current = baseConditional;

            while (true)
            {
                if (current is ConditionalContext conditionalContext)
                {
                    conditionalParents.Push(conditionalContext);
                    current = conditionalContext.Parent;
                }
                else
                {
                    break;
                }
            }

            var currentRoot = newRoot;
            while (conditionalParents.TryPop(out var oldConditionalContext))
            {
                currentRoot = currentRoot.GetOrCreateConditionalContext(oldConditionalContext.Conditional);
            }

            // TODO: Improve
            return (ConditionalContext)currentRoot;
        }

        public virtual BaseContext? GetOrCreateFieldContext(FieldNode fieldNode, ITypeDefinition fieldType)
        {
            if (fieldNode.SelectionSet is not null)
            {
                FieldContexts ??= new Dictionary<FieldNode, Context>(FieldNodeComparer.Instance);

                if (!FieldContexts.TryGetValue(fieldNode, out var fieldContext))
                {
                    fieldContext = new Context(this, fieldType, fragmentLookup);
                    FieldContexts[fieldNode] = fieldContext;

                    AddSelection(fieldNode);
                }

                return fieldContext;
            }
            else
            {
                AddSelection(fieldNode);

                return null;
            }
        }

        public ConditionalContext GetOrCreateConditionalContext(Conditional conditional)
        {
            ConditionalContexts ??= new Dictionary<Conditional, ConditionalContext>(ConditionalComparer.Instance);

            if (!ConditionalContexts.TryGetValue(conditional, out var conditionalContext))
            {
                var unconditionalContext = this switch
                {
                    ConditionalContext thisConditionalContext => thisConditionalContext.UnconditionalContext,
                    Context thisContext => thisContext,
                    _ => throw new NotSupportedException()
                };

                conditionalContext = new ConditionalContext(
                    this,
                    unconditionalContext,
                    conditional,
                    Type,
                    fragmentLookup);

                ConditionalContexts[conditional] = conditionalContext;
            }

            return conditionalContext;
        }

        protected void AddSelection(ISelectionNode selection)
        {
            Selections ??= new HashSet<ISelectionNode>(SyntaxNodeComparer.Instance);
            Selections.Add(selection);
        }

        protected internal void RemoveSelection(ISelectionNode selection)
        {
            Selections?.Remove(selection);

            if (selection is FieldNode fieldNode)
            {
                FieldContexts?.Remove(fieldNode);
            }
            else if (selection is InlineFragmentNode inlineFragmentNode)
            {
                FragmentContexts?.Remove(inlineFragmentNode);
            }
        }
    }

    private sealed class Context(
        BaseContext? parent,
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
        : BaseContext(parent, type, fragmentLookup)
    {
        public Dictionary<ISelectionNode, List<ConditionalContext>>? ConditionalParentContexts { get; set; }

        public override BaseContext? GetOrCreateFieldContext(FieldNode fieldNode, ITypeDefinition fieldType)
        {
            // This already takes core of the case of there not being a selection set.
            var fieldContext = base.GetOrCreateFieldContext(fieldNode, fieldType);

            if (ConditionalParentContexts is not null
                && ConditionalParentContexts.TryGetValue(fieldNode, out var conditionalContexts))
            {
                foreach (var conditionalContext in conditionalContexts)
                {
                    if (fieldContext is not null
                        && conditionalContext.FieldContexts is not null
                        && conditionalContext.FieldContexts.TryGetValue(fieldNode, out var conditionalFieldContext))
                    {
                        var conditionalContextBelowUnconditionalField =
                            RecreateConditionalContextHierarchy(fieldContext, conditionalContext);

                        MergeContexts(conditionalFieldContext, conditionalContextBelowUnconditionalField);
                    }

                    conditionalContext.RemoveSelection(fieldNode);
                }

                ConditionalParentContexts.Remove(fieldNode);
            }

            return fieldContext;
        }

        public override BaseContext GetOrCreateFragmentContext(
            InlineFragmentNode inlineFragmentNode,
            ITypeDefinition typeCondition)
        {
            var fragmentContext = base.GetOrCreateFragmentContext(inlineFragmentNode, typeCondition);

            if (ConditionalParentContexts is not null
                && ConditionalParentContexts.TryGetValue(inlineFragmentNode, out var conditionalContexts))
            {
                foreach (var conditionalContext in conditionalContexts)
                {
                    if (conditionalContext.FragmentContexts is not null
                        && conditionalContext.FragmentContexts.TryGetValue(inlineFragmentNode, out var conditionalFragmentContext))
                    {
                        var conditionalContextBelowUnconditionalFragment =
                            RecreateConditionalContextHierarchy(fragmentContext, conditionalContext);

                        MergeContexts(conditionalFragmentContext, conditionalContextBelowUnconditionalFragment);
                    }

                    conditionalContext.RemoveSelection(inlineFragmentNode);
                }

                ConditionalParentContexts.Remove(inlineFragmentNode);
            }

            return fragmentContext;
        }

        private void MergeContexts(BaseContext source, BaseContext target)
        {
        }
    }

    private sealed class ConditionalContext(
        BaseContext parent,
        Context unconditionalContext,
        Conditional conditional,
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
        : BaseContext(parent, type, fragmentLookup)
    {
        public Context UnconditionalContext { get; } = unconditionalContext;

        public Conditional Conditional { get; } = conditional;

        public override BaseContext? GetOrCreateFieldContext(FieldNode fieldNode, ITypeDefinition fieldType)
        {
            if (UnconditionalContext.Selections?.Contains(fieldNode) == true)
            {
                if (fieldNode.SelectionSet is null)
                {
                    return null;
                }

                if (UnconditionalContext.FieldContexts!.TryGetValue(fieldNode, out var unconditionalFieldContext))
                {
                    // TODO: This should actually recreate the entire conditional hierarchy up until the first non-conditional parent
                    var conditionalContext = unconditionalFieldContext.GetOrCreateConditionalContext(Conditional);

                    return conditionalContext;
                }
                else
                {
                    throw new InvalidOperationException(
                        "Expected to be able to a find a field context, if the selection exists.");
                }
            }

            UnconditionalContext.ConditionalParentContexts ??=
                new Dictionary<ISelectionNode, List<ConditionalContext>>(SyntaxNodeComparer.Instance);

            if (!UnconditionalContext.ConditionalParentContexts.TryGetValue(fieldNode, out var conditionalContexts))
            {
                conditionalContexts = [];
                UnconditionalContext.ConditionalParentContexts[fieldNode] = conditionalContexts;
            }

            conditionalContexts.Add(this);

            return base.GetOrCreateFieldContext(fieldNode, fieldType);
        }

        public override BaseContext GetOrCreateFragmentContext(
            InlineFragmentNode inlineFragmentNode,
            ITypeDefinition typeCondition)
        {
            if (UnconditionalContext.FragmentContexts is not null
                && UnconditionalContext.FragmentContexts.TryGetValue(inlineFragmentNode, out var unconditionalFragmentContext))
            {
                // TODO: This should actually recreate the entire conditional hierarchy up until the first non-conditional parent
                var conditionalContext = unconditionalFragmentContext.GetOrCreateConditionalContext(Conditional);

                return conditionalContext;
            }

            UnconditionalContext.ConditionalParentContexts ??=
                new Dictionary<ISelectionNode, List<ConditionalContext>>(SyntaxNodeComparer.Instance);

            if (!UnconditionalContext.ConditionalParentContexts.TryGetValue(inlineFragmentNode, out var conditionalContexts))
            {
                conditionalContexts = [];
                UnconditionalContext.ConditionalParentContexts[inlineFragmentNode] = conditionalContexts;
            }

            conditionalContexts.Add(this);

            return base.GetOrCreateFragmentContext(inlineFragmentNode, typeCondition);
        }
    }

    private sealed class Conditional
    {
        public DirectiveNode? Skip { get; set; }

        public DirectiveNode? Include { get; set; }

        public IReadOnlyList<DirectiveNode> ToDirectives()
        {
            var builder = ImmutableArray.CreateBuilder<DirectiveNode>();

            if (Skip is not null)
            {
                builder.Add(Skip);
            }

            if (Include is not null)
            {
                builder.Add(Include);
            }

            return builder.ToImmutable();
        }

        public override string ToString()
        {
            var skipDirective = Skip?.ToString();
            var includeDirective = Include?.ToString();

            if (skipDirective is not null && includeDirective is not null)
            {
                return $"{skipDirective} {includeDirective}";
            }

            if (skipDirective is not null)
            {
                return skipDirective;
            }

            if (includeDirective is not null)
            {
                return includeDirective;
            }

            throw new InvalidOperationException();
        }
    }

     private sealed class ConditionalComparer : IEqualityComparer<Conditional>
    {
        private static readonly IEqualityComparer<ISyntaxNode> s_comparer = SyntaxComparer.BySyntax;

        public bool Equals(Conditional? x, Conditional? y)
        {
            return s_comparer.Equals(x?.Skip, y?.Skip) && s_comparer.Equals(x?.Include, y?.Include);
        }

        public int GetHashCode(Conditional obj)
        {
            return HashCode.Combine(GetDirectiveHashCode(obj.Skip), GetDirectiveHashCode(obj.Include));
        }

        private static int GetDirectiveHashCode(DirectiveNode? node)
        {
            return node is null ? 0 : s_comparer.GetHashCode(node);
        }

        public static ConditionalComparer Instance { get; } = new();
    }

    private sealed class SyntaxNodeComparer : IEqualityComparer<ISyntaxNode>
    {
        public bool Equals(ISyntaxNode? x, ISyntaxNode? y)
        {
            if (x is FieldNode xField && y is FieldNode yField)
            {
                return FieldNodeComparer.Instance.Equals(xField, yField);
            }

            if (x is InlineFragmentNode xFragment && y is InlineFragmentNode yFragment)
            {
                return InlineFragmentNodeComparer.Instance.Equals(xFragment, yFragment);
            }

            return false;
        }

        public int GetHashCode(ISyntaxNode obj)
        {
            if (obj is FieldNode field)
            {
                return FieldNodeComparer.Instance.GetHashCode(field);
            }

            if (obj is InlineFragmentNode inlineFragment)
            {
                return InlineFragmentNodeComparer.Instance.GetHashCode(inlineFragment);
            }

            throw new NotImplementedException();
        }

        public static SyntaxNodeComparer Instance { get; } = new();
    }

    private sealed class InlineFragmentNodeComparer : IEqualityComparer<InlineFragmentNode>
    {
        public bool Equals(InlineFragmentNode? x, InlineFragmentNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            return SyntaxComparer.BySyntax.Equals(x.TypeCondition, y.TypeCondition)
                && Equals(x.Directives, y.Directives);
        }

        private bool Equals(IReadOnlyList<ISyntaxNode> a, IReadOnlyList<ISyntaxNode> b)
        {
            if (a.Count == 0 && b.Count == 0)
            {
                return true;
            }

            return a.SequenceEqual(b, SyntaxComparer.BySyntax);
        }

        public int GetHashCode(InlineFragmentNode obj)
        {
            var hashCode = new HashCode();

            if (obj.TypeCondition is not null)
            {
                hashCode.Add(obj.TypeCondition.Name.Value);
            }

            for (var i = 0; i < obj.Directives.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Directives[i]));
            }

            return hashCode.ToHashCode();
        }

        public static InlineFragmentNodeComparer Instance { get; } = new();
    }

    private sealed class FieldNodeComparer : IEqualityComparer<FieldNode>
    {
        public bool Equals(FieldNode? x, FieldNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            return Equals(x.Alias, y.Alias)
                && x.Name.Equals(y.Name)
                && Equals(x.Directives, y.Directives)
                && Equals(x.Arguments, y.Arguments);
        }

        private bool Equals(IReadOnlyList<ISyntaxNode> a, IReadOnlyList<ISyntaxNode> b)
        {
            if (a.Count == 0 && b.Count == 0)
            {
                return true;
            }

            return a.SequenceEqual(b, SyntaxComparer.BySyntax);
        }

        public int GetHashCode(FieldNode obj)
        {
            var hashCode = new HashCode();

            if (obj.Alias is not null)
            {
                hashCode.Add(obj.Alias.Value);
            }

            hashCode.Add(obj.Name.Value);

            for (var i = 0; i < obj.Directives.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Directives[i]));
            }

            for (var i = 0; i < obj.Arguments.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Arguments[i]));
            }

            return hashCode.ToHashCode();
        }

        public static FieldNodeComparer Instance { get; } = new();
    }
}
