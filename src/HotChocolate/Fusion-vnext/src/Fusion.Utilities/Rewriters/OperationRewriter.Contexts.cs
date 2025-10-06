using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed partial class OperationRewriter
{
    private abstract class BaseContext(
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
    {
        public ITypeDefinition Type { get; } = type;

        public Dictionary<Conditional, ConditionalContext>? ConditionalContexts { get; set; }

        public Dictionary<FieldNode, Context>? FieldContexts { get; set; }

        public Dictionary<InlineFragmentNode, Context>? FragmentContexts { get; set; }

        public HashSet<ISelectionNode>? Selections { get; set; }

        public FragmentDefinitionNode GetFragmentDefinition(string name)
            => fragmentLookup[name];

        public void AddSelection(ISelectionNode selection)
        {
            Selections ??= new HashSet<ISelectionNode>(SyntaxNodeComparer.Instance);
            Selections.Add(selection);
        }

        public virtual BaseContext GetOrCreateFragmentContext(
            InlineFragmentNode inlineFragmentNode,
            ITypeDefinition typeCondition)
        {
            FragmentContexts ??= new Dictionary<InlineFragmentNode, Context>(InlineFragmentNodeComparer.Instance);

            if (!FragmentContexts.TryGetValue(inlineFragmentNode, out var fragmentContext))
            {
                fragmentContext = new Context(typeCondition, fragmentLookup);
                FragmentContexts[inlineFragmentNode] = fragmentContext;

                AddSelection(inlineFragmentNode);
            }

            return fragmentContext;
        }

        public virtual BaseContext? AddField(FieldNode fieldNode, ITypeDefinition fieldType)
        {
            if (fieldNode.SelectionSet is not null)
            {
                FieldContexts ??= new Dictionary<FieldNode, Context>(FieldNodeComparer.Instance);

                if (!FieldContexts.TryGetValue(fieldNode, out var fieldContext))
                {
                    fieldContext = new Context(fieldType, fragmentLookup);
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

                conditionalContext = new ConditionalContext(unconditionalContext, conditional, Type, fragmentLookup);
                ConditionalContexts[conditional] = conditionalContext;
            }

            return conditionalContext;
        }
    }

    private sealed class Context(
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
        : BaseContext(type, fragmentLookup)
    {
        public override BaseContext GetOrCreateFragmentContext(
            InlineFragmentNode inlineFragmentNode,
            ITypeDefinition typeCondition)
        {
            // TODO: Remove associated conditionals and merge their contexts
            //       This action is basically the same as for the conditional one
            return base.GetOrCreateFragmentContext(inlineFragmentNode, typeCondition);
        }
    }

    private sealed class ConditionalContext(
        Context unconditionalContext,
        Conditional conditional,
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
        : BaseContext(type, fragmentLookup)
    {
        public Context UnconditionalContext { get; } = unconditionalContext;

        public override BaseContext GetOrCreateFragmentContext(
            InlineFragmentNode inlineFragmentNode,
            ITypeDefinition typeCondition)
        {
            if (UnconditionalContext.FragmentContexts is not null
                && UnconditionalContext.FragmentContexts.TryGetValue(inlineFragmentNode, out var unconditionalFragmentContext))
            {
                // TODO: This should actually recreate the entire conditional hierarchy up until the first non-conditional parent
                var conditionalContext = unconditionalFragmentContext.GetOrCreateConditionalContext(conditional);

                return conditionalContext;
            }

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
