using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed partial class OperationRewriter(
    ISchemaDefinition schema,
    bool removeStaticallyExcludedSelections = false)
{
    public DocumentNode RewriteDocument(DocumentNode document, string? operationName = null)
    {
        var operation = document.GetOperation(operationName);
        var operationType = schema.GetOperationType(operation.Operation);
        var fragmentLookup = CreateFragmentLookup(document);
        var context = new Context(null, operationType, fragmentLookup);

        CollectSelections(operation.SelectionSet, context);

        var newSelections = RewriteSelections(context);

        // TODO: Handle empty case better
        var newSelectionSet = new SelectionSetNode(newSelections ?? []);

        var newOperation = new OperationDefinitionNode(
            null,
            operation.Name,
            operation.Description,
            operation.Operation,
            operation.VariableDefinitions,
            RewriteDirectives(operation.Directives),
            newSelectionSet);

        return new DocumentNode([newOperation]);
    }

    private static bool IsStaticallySkipped(IHasDirectives directiveProvider)
    {
        if (directiveProvider.Directives.Count == 0)
        {
            return false;
        }

        foreach (var directive in directiveProvider.Directives)
        {
            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal)
                && directive.Arguments is [{ Value: BooleanValueNode { Value: true } }])
            {
                return true;
            }

            if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal)
                && directive.Arguments is [{ Value: BooleanValueNode { Value: false } }])
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, FragmentDefinitionNode> CreateFragmentLookup(DocumentNode document)
    {
        var lookup = new Dictionary<string, FragmentDefinitionNode>();

        foreach (var definition in document.Definitions)
        {
            if (definition is FragmentDefinitionNode fragmentDef)
            {
                lookup.Add(fragmentDef.Name.Value, fragmentDef);
            }
        }

        return lookup;
    }
}
