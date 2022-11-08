using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.PrepareDocuments;

public class PrepareDocumentsMiddleware
{
    private readonly MergeSchema _next;

    public PrepareDocumentsMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        foreach (var configuration in context.Configurations)
        {
            var definitions = new List<IDefinitionNode>();

            RegisterServiceInfo(definitions, configuration);

            foreach (var document in configuration.Documents)
            {
                foreach (var definition in document.Definitions)
                {
                    definitions.Add(definition);
                }
            }

            context.Documents = context.Documents.Add(
                new Document(configuration.Name, new DocumentNode(definitions)));
        }

        await _next(context);
    }

    private static void RegisterServiceInfo(
        List<IDefinitionNode> definitions,
        ServiceConfiguration configuration)
    {
        var serviceName = new DirectiveNode(
            "_hc_service",
            new ArgumentNode("name", configuration.Name));

        var serviceInfo = new SchemaExtensionNode(
            null,
            new[] { serviceName },
            Array.Empty<OperationTypeDefinitionNode>());

        definitions.Add(serviceInfo);
    }
}
