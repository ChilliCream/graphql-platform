using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.Projections.ProjectionConvention;
using static HotChocolate.Execution.Processing.OperationCompilerOptimizerHelper;

namespace HotChocolate.Data.Projections;

internal sealed class ProjectionTypeInterceptor : TypeInterceptor
{
    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if ((completionContext.IsQueryType ?? false) &&
            completionContext.Type is ObjectType { Fields: var fields })
        {
            var foundNode = false;
            var foundNodes = false;

            foreach (var field in fields)
            {
                if (field.Name is "node" or "nodes")
                {
                    if (field.Name is "node") { foundNode = true; }

                    if (field.Name is "nodes") { foundNodes = true; }

                    var selectionOptimizer = completionContext.DescriptorContext
                        .GetProjectionConvention()
                        .CreateOptimizer();

                    if (field.ContextData is not ExtensionData extensionData)
                    {
                        throw ThrowHelper.ProjectionConvention_NodeFieldWasInInvalidState();
                    }

                    RegisterOptimizer(
                        extensionData,
                        new NodeSelectionSetOptimizer(selectionOptimizer));

                    if (foundNode && foundNodes)
                    {
                        break;
                    }
                }
            }
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is ObjectTypeDefinition objectTypeDefinition)
        {
            List<string>? alwaysProjected = null;
            foreach (var field in objectTypeDefinition.Fields)
            {
                alwaysProjected ??= new List<string>();
                if (field.GetContextData().TryGetValue(IsProjectedKey, out var value) &&
                    value is true)
                {
                    alwaysProjected.Add(field.Name);
                }
            }

            if (alwaysProjected?.Count > 0)
            {
                definition.ContextData[AlwaysProjectedFieldsKey] = alwaysProjected.ToArray();
            }
        }
    }
}
