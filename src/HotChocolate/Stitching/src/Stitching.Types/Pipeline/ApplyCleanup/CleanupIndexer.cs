using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyCleanup;

internal sealed class CleanupIndexer : SyntaxWalker<CleanupContext>
{
    protected override ISyntaxVisitorAction Enter(ISyntaxNode node, CleanupContext context)
    {
        context.Navigator.Push(node);

        switch (node)
        {
            case ComplexTypeDefinitionNodeBase type:
            {
                if (type.Directives.Count > 0)
                {
                    break;
                }

                if (type.Interfaces.Count > 0)
                {
                    break;
                }

                if (type.Fields.Count > 0)
                {
                    break;
                }

                context.Types[type.Name.Value] = new CleanupInfo(type);
                break;
            }

            case UnionTypeDefinitionNode type:
                if (type.Directives.Count > 0)
                {
                    break;
                }

                if (type.Types.Count > 0)
                {
                    break;
                }

                context.Types[type.Name.Value] = new CleanupInfo(type);
                break;

            case InputObjectTypeDefinitionNode type:
                if (type.Directives.Count > 0)
                {
                    break;
                }

                if (type.Fields.Count > 0)
                {
                    break;
                }

                context.Types[type.Name.Value] = new CleanupInfo(type);
                break;

            case EnumTypeDefinitionNode type:
                if (type.Directives.Count > 0)
                {
                    break;
                }

                if (type.Values.Count > 0)
                {
                    break;
                }

                context.Types[type.Name.Value] = new CleanupInfo(type);
                break;

            case ScalarTypeDefinitionNode type:
                if (type.Directives.Count > 0)
                {
                    break;
                }

                context.Types[type.Name.Value] = new CleanupInfo(type);
                break;

            case ScalarTypeExtensionNode type:
                if (type.Directives.Count > 0)
                {
                    break;
                }

                context.Types[type.Name.Value] = new CleanupInfo(type);
                break;
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(ISyntaxNode node, CleanupContext context)
    {
        context.Navigator.Pop();
        return base.Leave(node, context);
    }
}
