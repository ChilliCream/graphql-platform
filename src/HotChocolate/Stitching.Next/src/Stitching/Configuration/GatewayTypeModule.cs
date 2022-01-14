using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Factories;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Stitching.SchemaBuilding;

internal sealed class GatewayTypeModule : ITypeModule
{
    private readonly SchemaInspector _schemaInspector = new();
    private readonly SchemaMerger _schemaMerger = new();
    private readonly ISchemaDocumentResolver[] _schemaDocumentResolvers;

    public event EventHandler<EventArgs>? TypesChanged;

    public GatewayTypeModule(IEnumerable<ISchemaDocumentResolver> schemaDocumentResolvers)
    {
        if (schemaDocumentResolvers is null)
        {
            throw new ArgumentNullException(nameof(schemaDocumentResolvers));
        }

        _schemaDocumentResolvers = schemaDocumentResolvers.ToArray();
    }

    public async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
    {
        HashSet<NameString> sources = new();
        List<SchemaInfo> schemaInfos = new();

        foreach (ISchemaDocumentResolver schemaDocumentResolver in _schemaDocumentResolvers)
        {
            var schemaDocument =
                await schemaDocumentResolver.GetDocumentAsync(cancellationToken)
                    .ConfigureAwait(false);

            SchemaInfo schemaInfo = _schemaInspector.Inspect(schemaDocument);

            sources.Add(schemaInfo.Name);
            schemaInfos.Add(schemaInfo);
        }

        SchemaInfo merged = _schemaMerger.Merge(schemaInfos);
        context.ContextData.Add(StitchingContextData.Sources, sources);
        context.ContextData.Add(StitchingContextData.SchemaInfo, merged);

        var typeVisitor = new SchemaSyntaxVisitor();
        var typeContext = new SchemaSyntaxVisitorContext(context);

        if (merged.Query is not null)
        {
            typeVisitor.Visit(merged.Query.Definition, typeContext);
            context.ContextData[QueryName] = merged.Query.Name.Value;
        }

        if (merged.Mutation is not null)
        {
            typeVisitor.Visit(merged.Mutation.Definition, typeContext);
            context.ContextData[MutationName] = merged.Mutation.Name.Value;
        }

        if (merged.Subscription is not null)
        {
            typeVisitor.Visit(merged.Subscription.Definition, typeContext);
            context.ContextData[SubscriptionName] = merged.Subscription.Name.Value;
        }

        foreach (ITypeInfo typeInfo in merged.Types.Values)
        {
            typeVisitor.Visit(typeInfo.Definition, typeContext);
        }

        return typeContext.Types.OfType<SchemaTypeReference>().Select(t => t.Type).ToArray();
    }
}
