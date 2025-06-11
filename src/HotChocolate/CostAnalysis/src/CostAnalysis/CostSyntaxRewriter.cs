using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Removes <c>@cost</c> directives with default weight from the SDL.
/// </summary>
internal sealed class CostSyntaxRewriter : SyntaxRewriter<CostSyntaxRewriter.Context>
{
    protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        Context context)
    {
        context.Types.Push(context.Schema.Types.GetType<ITypeDefinition>(node.Name.Value));
        node = base.RewriteObjectTypeDefinition(node, context)!;
        context.Types.Pop();

        return node;
    }

    protected override InterfaceTypeDefinitionNode RewriteInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        Context context)
    {
        context.Types.Push(context.Schema.Types.GetType<ITypeDefinition>(node.Name.Value));
        node = base.RewriteInterfaceTypeDefinition(node, context)!;
        context.Types.Pop();

        return node;
    }

    protected override FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        Context context)
    {
        var field = ((IComplexTypeDefinition)context.Types.Peek()).Fields[node.Name.Value];

        context.Types.Push(field);
        node = base.RewriteFieldDefinition(node, context)!;
        context.Types.Pop();

        return node;
    }

    protected override InputObjectTypeDefinitionNode RewriteInputObjectTypeDefinition(
        InputObjectTypeDefinitionNode node,
        Context context)
    {
        context.Types.Push(context.Schema.Types.GetType<ITypeDefinition>(node.Name.Value));
        node = base.RewriteInputObjectTypeDefinition(node, context)!;
        context.Types.Pop();

        return node;
    }

    protected override InputValueDefinitionNode RewriteInputValueDefinition(
        InputValueDefinitionNode node,
        Context context)
    {
        var inputField = context.Types.Peek() switch
        {
            DirectiveType directiveType => directiveType.Arguments[node.Name.Value],
            IOutputFieldDefinition outputField => outputField.Arguments[node.Name.Value],
            InputObjectType inputObjectType => inputObjectType.Fields[node.Name.Value],
            _ => throw new InvalidOperationException()
        };

        context.Types.Push(inputField);
        node = base.RewriteInputValueDefinition(node, context)!;
        context.Types.Pop();

        return node;
    }

    protected override EnumTypeDefinitionNode RewriteEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        Context context)
    {
        context.Types.Push(context.Schema.Types.GetType<ITypeDefinition>(node.Name.Value));
        node = base.RewriteEnumTypeDefinition(node, context)!;
        context.Types.Pop();

        return node;
    }

    protected override ScalarTypeDefinitionNode RewriteScalarTypeDefinition(
        ScalarTypeDefinitionNode node,
        Context context)
    {
        context.Types.Push(context.Schema.Types.GetType<ITypeDefinition>(node.Name.Value));
        node = base.RewriteScalarTypeDefinition(node, context)!;
        context.Types.Pop();

        return node;
    }

    protected override DirectiveDefinitionNode RewriteDirectiveDefinition(
        DirectiveDefinitionNode node,
        Context context)
    {
        context.Types.Push(context.Schema.DirectiveDefinitions[node.Name.Value]);
        node = base.RewriteDirectiveDefinition(node, context)!;
        context.Types.Pop();

        return node;
    }

    protected override DirectiveNode? RewriteDirective(DirectiveNode node, Context context)
    {
        if (node.Name.Value != WellKnownDirectiveNames.Cost)
        {
            return node;
        }

        var typeSystemMember = context.Types.Peek();

        IType type = typeSystemMember switch
        {
            Argument argument => argument.Type,
            EnumType enumType => enumType,
            IInputValueDefinition field => field.Type,
            IOutputFieldDefinition field => field.Type,
            ObjectType objectType => objectType,
            ScalarType scalarType => scalarType,
            _ => throw new InvalidOperationException()
        };

        var directive = ((IDirectivesProvider)typeSystemMember).Directives[WellKnownDirectiveNames.Cost].Single();
        var costWeight = ((Directive)directive).GetArgumentValue<double>(WellKnownArgumentNames.Weight);

        if (type.IsLeafType() && costWeight == 0.0)
        {
            return null;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if ((type.IsCompositeType() || type.IsInputObjectType() || type.IsListType())
            && costWeight == 1.0)
        {
            return null;
        }

        return base.RewriteDirective(node, context);
    }

    public sealed class Context(ISchemaDefinition schema)
    {
        public ISchemaDefinition Schema { get; } = schema;

        public Stack<ITypeSystemMember> Types { get; } = new();
    }
}
