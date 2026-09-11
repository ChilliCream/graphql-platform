using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Fusion.Execution.Validation;

/// <summary>
/// Rejects operations that declare or use a variable whose name starts with the reserved
/// <c>__fusion</c> prefix. The Fusion gateway uses this prefix for variables it
/// generates internally, so a document-defined variable with this prefix could
/// collide with, and override, an internally generated value. This rule is active
/// for every request, regardless of whether the schema declares any policies.
/// </summary>
/// <remarks>
/// The prefix is rejected everywhere a variable can be declared, operation variable
/// definitions as well as fragment variable definitions, and everywhere it can be
/// used, field and directive arguments and their nested list and object values. The
/// usage check is a belt-and-braces measure on top of the standard undefined-variable
/// validation: a fragment variable definition puts its variable in scope for the
/// fragment body, so a reserved-prefixed usage there would not otherwise be flagged
/// as undefined.
/// </remarks>
internal sealed class ReservedVariablePrefixRule : IDocumentValidatorRule
{
    private const string ReservedPrefix = "__fusion";

    public ushort Priority => ushort.MaxValue;

    public bool IsCacheable => true;

    public void Validate(DocumentValidatorContext context, DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(document);

        var definitions = document.Definitions;

        for (var i = 0; i < definitions.Count; i++)
        {
            switch (definitions[i])
            {
                case OperationDefinitionNode operation:
                    ValidateVariableDefinitions(
                        context,
                        operation.VariableDefinitions,
                        isFragment: false);
                    ValidateVariableUsages(context, operation.Directives);
                    ValidateVariableUsages(context, operation.SelectionSet);
                    break;

                case FragmentDefinitionNode fragment:
                    ValidateVariableDefinitions(
                        context,
                        fragment.VariableDefinitions,
                        isFragment: true);
                    ValidateVariableUsages(context, fragment.Directives);
                    ValidateVariableUsages(context, fragment.SelectionSet);
                    break;
            }
        }
    }

    private static void ValidateVariableDefinitions(
        DocumentValidatorContext context,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        bool isFragment)
    {
        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var variableDefinition = variableDefinitions[i];
            var variableName = variableDefinition.Variable.Name.Value;

            if (!variableName.StartsWith(ReservedPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var error = isFragment
                ? ErrorHelper.ReservedVariablePrefixInFragment(variableDefinition, variableName)
                : ErrorHelper.ReservedVariablePrefix(variableDefinition, variableName);

            context.ReportError(error);
        }
    }

    private static void ValidateVariableUsages(
        DocumentValidatorContext context,
        IReadOnlyList<DirectiveNode> directives)
    {
        for (var i = 0; i < directives.Count; i++)
        {
            ValidateVariableUsages(context, (ISyntaxNode)directives[i]);
        }
    }

    private static void ValidateVariableUsages(DocumentValidatorContext context, ISyntaxNode node)
    {
        if (node is VariableNode variable)
        {
            var variableName = variable.Name.Value;

            if (variableName.StartsWith(ReservedPrefix, StringComparison.Ordinal))
            {
                context.ReportError(ErrorHelper.ReservedVariablePrefixUsage(variable, variableName));
            }

            return;
        }

        foreach (var child in node.GetNodes())
        {
            ValidateVariableUsages(context, child);
        }
    }
}
