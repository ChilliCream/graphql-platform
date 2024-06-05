using HotChocolate.Configuration;
using HotChocolate.CostAnalysis.Properties;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.CostAnalysis.WellKnownContextData;

namespace HotChocolate.CostAnalysis;

internal sealed class CostIntrospectionTypeInterceptor : TypeInterceptor
{
    /// <summary>Gets the field name of the __cost introspection field.</summary>
    public const string Cost = "__cost";

    private IDescriptorContext _context = default!;
    private ObjectTypeDefinition? _queryTypeDefinition;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _context = context;
    }

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryTypeDefinition = definition;
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        _queryTypeDefinition?.Fields.Insert(0, CreateCostField(_context));
    }

    private static ObjectFieldDefinition CreateCostField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, Cost);

        descriptor
            .Type<CostType>()
            .Description(CostAnalysisResources.CostType_Description);

        var definition = descriptor.Definition;
        definition.IsIntrospectionField = true;
        definition.PureResolver = Resolver;

        return definition;

        static Cost Resolver(IPureResolverContext ctx)
        {
            var requestCosts = (CostMetrics)ctx.ContextData[RequestCosts]!;

            return new Cost(requestCosts);
        }
    }
}
