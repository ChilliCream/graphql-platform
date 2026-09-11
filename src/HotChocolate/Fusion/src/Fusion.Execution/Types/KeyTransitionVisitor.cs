using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

internal sealed class KeyTransitionVisitor : SyntaxWalker<KeyTransitionVisitor.Context>
{
    protected override ISyntaxVisitorAction Enter(FieldNode node, Context context)
    {
        var type = (FusionComplexTypeDefinition)context.Types.Peek();
        var field = type.Fields.GetField(node.Name.Value, allowInaccessibleFields: true);

        if (!field.Sources.TryGetMember(context.SourceSchema, out var member) || member.Requirements is not null)
        {
            context.NeedsTransition = true;
            return Break;
        }

        context.Fields++;
        context.Types.Push(field.Type.NamedType());
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(FieldNode node, Context context)
    {
        context.Types.Pop();
        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(InlineFragmentNode node, Context context)
    {
        context.Fragments++;

        if (node.TypeCondition is { Name: { } typeName })
        {
            context.Types.Push(context.CompositeSchema.Types[typeName.Value]);
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(InlineFragmentNode node, Context context)
    {
        if (node.TypeCondition is not null)
        {
            context.Types.Pop();
        }

        return base.Leave(node, context);
    }

    public sealed class Context
    {
        public required FusionSchemaDefinition CompositeSchema { get; init; }

        public required string SourceSchema { get; init; }

        public required List<ITypeDefinition> Types { get; init; }

        public bool NeedsTransition { get; set; }

        public int Fields { get; set; }

        public int Fragments { get; set; }

        public void Reset()
        {
            var first = Types[0];
            Types.Clear();
            NeedsTransition = false;
            Fields = 0;
            Fragments = 0;
            Types.Push(first);
        }
    }
}
