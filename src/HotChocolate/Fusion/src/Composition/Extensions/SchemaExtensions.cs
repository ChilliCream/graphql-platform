using System.Text;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal static class SchemaExtensions
{
    private static readonly FieldVariableNameVisitor _fieldVariableNameVisitor = new();

    public static string CreateVariableName(
        this ObjectType type,
        IsDirective directive)
        => directive.IsCoordinate
            ? CreateVariableName(type, directive.Coordinate.Value)
            : CreateVariableName(type, directive.Field);

    private static string CreateVariableName(
        ObjectType type,
        SchemaCoordinate coordinate)
        => $"{type.Name}_{coordinate.MemberName}";

    private static string CreateVariableName(
        ObjectType type,
        FieldNode field)
    {
        var context = new FieldVariableNameContext();
        _fieldVariableNameVisitor.Visit(field, context);
        context.Name.Insert(0, type.Name);
        return context.Name.ToString();
    }

    public static VariableDefinition CreateVariableField(
        this InputField argument,
        IsDirective directive,
        string variableName)
    {
        var field = directive.IsCoordinate
            ? new FieldNode(
                null,
                new NameNode(directive.Coordinate.Value.MemberName!),
                null,
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null)
            : directive.Field;

        var variable = new VariableDefinitionNode(
            null,
            new VariableNode(variableName),
            argument.Type.ToTypeNode(),
            null,
            Array.Empty<DirectiveNode>());

        return new VariableDefinition(variableName, field, variable);
    }

    private sealed class FieldVariableNameVisitor : SyntaxWalker<FieldVariableNameContext>
    {
        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            FieldVariableNameContext context)
        {
            context.Name.Append('_');
            context.Name.Append(node.Name.Value);
            return Continue;
        }
    }

    private sealed class FieldVariableNameContext : ISyntaxVisitorContext
    {
        public StringBuilder Name { get; } = new();
    }
}
