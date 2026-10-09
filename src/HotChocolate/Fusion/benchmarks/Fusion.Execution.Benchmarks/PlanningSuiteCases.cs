using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// The planning cases measured by <see cref="PlanningSuiteBenchmark"/>. Each case plans one
/// operation against one composed schema with its own planner.
/// </summary>
internal sealed class PlanningSuiteCases
{
    private static readonly Lazy<PlanningSuiteCases> s_instance = new(() => new PlanningSuiteCases());

    private readonly Dictionary<string, PlanningSuiteCase> _cases = [];

    private PlanningSuiteCases()
    {
        var shop = ComposeSchema(
            "shop-00.graphqls",
            "shop-01.graphqls",
            "shop-02.graphqls",
            "shop-03.graphqls",
            "shop-04.graphqls",
            "shop-05.graphqls",
            "shop-06.graphqls");
        var k6 = ComposeSchema(
            "k6-accounts.graphqls",
            "k6-inventory.graphqls",
            "k6-products.graphqls",
            "k6-reviews.graphqls");
        var k6Composite = LoadSchema("composite-k6.graphqls");
        var requirementsShop = LoadSchema("composite-requirements-shop.graphqls");
        var wideDetail = LoadSchema("composite-wide-detail.graphqls");

        ShopMediumQueryWithAliases = Add(
            nameof(PlanningSuiteBenchmark.Shop_Medium_Query_With_Aliases),
            shop,
            "shop-medium-query-with-aliases.graphql");

        K6DeepNesting = Add(
            nameof(PlanningSuiteBenchmark.K6_DeepNesting),
            k6,
            "k6.graphql");

        MergePolicyAggressiveK6 = Add(
            nameof(PlanningSuiteBenchmark.MergePolicy_Aggressive_K6),
            k6Composite,
            "k6.graphql",
            OperationPlannerOptions.Default);

        MergePolicyBalancedK6 = Add(
            nameof(PlanningSuiteBenchmark.MergePolicy_Balanced_K6),
            k6Composite,
            "k6.graphql",
            new OperationPlannerOptions { MergePolicy = OperationMergePolicy.Balanced });

        MergePolicyConservativeK6 = Add(
            nameof(PlanningSuiteBenchmark.MergePolicy_Conservative_K6),
            k6Composite,
            "k6.graphql",
            new OperationPlannerOptions { MergePolicy = OperationMergePolicy.Conservative });

        RequirementChainMergeRequirementLookups = Add(
            nameof(PlanningSuiteBenchmark.RequirementChain_Merge_Requirement_Lookups),
            LoadSchema("composite-requirement-chain.graphqls"),
            "requirement-chain-merge-requirement-lookups.graphql",
            new OperationPlannerOptions { OperationWeight = 100 });

        RequirementDirectiveLeaksShop = Add(
            nameof(PlanningSuiteBenchmark.Requirement_Directive_Leaks_Shop),
            requirementsShop,
            "requirement-directive-leaks-shop.graphql");

        RequirementSelectionMapObjectShop = Add(
            nameof(PlanningSuiteBenchmark.Requirement_SelectionMap_Object_Shop),
            requirementsShop,
            "requirement-selection-map-object-shop.graphql");

        RequirementPlanComplexOperation = Add(
            nameof(PlanningSuiteBenchmark.Requirement_Plan_Complex_Operation),
            requirementsShop,
            "requirement-plan-complex-operation.graphql");

        RequirementCrossEntityAuthorInputObjectList = Add(
            nameof(PlanningSuiteBenchmark.RequirementCrossEntity_Author_Input_Object_List),
            LoadSchema("composite-requirement-cross-entity.graphqls"),
            "requirement-cross-entity-author-input-object-list.graphql");

        PlannerBehaviorMutations = Add(
            nameof(PlanningSuiteBenchmark.PlannerBehavior_Mutations),
            LoadSchema("composite-mutations.graphqls"),
            "planner-behavior-mutations.graphql");

        WideDetailRepeatedFragmentSpreads = Add(
            nameof(PlanningSuiteBenchmark.WideDetail_Repeated_Fragment_Spreads),
            wideDetail,
            "wide-detail-repeated-fragment-spreads.graphql");

        WideDetailSingleFragmentSpreads = Add(
            nameof(PlanningSuiteBenchmark.WideDetail_Single_Fragment_Spreads),
            wideDetail,
            "wide-detail-single-fragment-spreads.graphql");

        InterfaceFieldOverrideSpill = Add(
            nameof(PlanningSuiteBenchmark.InterfaceFieldOverride_Spill),
            LoadSchema("composite-interface-field-override.graphqls"),
            "interface-field-override-spill.graphql");

        DeferStepInternalPredecessor = Add(
            nameof(PlanningSuiteBenchmark.Defer_Step_Internal_Predecessor),
            LoadSchema("composite-defer-predecessor.graphqls"),
            "defer-step-internal-predecessor.graphql");

        GuardrailDeferGroups = Add(
            nameof(PlanningSuiteBenchmark.Guardrail_Defer_Groups),
            LoadSchema("composite-guardrail-defer.graphqls"),
            "guardrail-defer-groups.graphql",
            new OperationPlannerOptions { MaxExpandedNodes = 32 });

        ShareableRoutingExcludeSourceExternalProvider = Add(
            nameof(PlanningSuiteBenchmark.ShareableRouting_Exclude_Source_External_Provider),
            LoadSchema("composite-shareable-routing.graphqls"),
            "shareable-routing-exclude-source-external-provider.graphql");
    }

    /// <summary>
    /// Gets the shared instance. The cases are composed and planned once on first access.
    /// </summary>
    public static PlanningSuiteCases Instance => s_instance.Value;

    /// <summary>
    /// Gets a value indicating whether the cases have been created in this process.
    /// </summary>
    public static bool IsCreated => s_instance.IsValueCreated;

    public PlanningSuiteCase ShopMediumQueryWithAliases { get; }

    public PlanningSuiteCase K6DeepNesting { get; }

    public PlanningSuiteCase MergePolicyAggressiveK6 { get; }

    public PlanningSuiteCase MergePolicyBalancedK6 { get; }

    public PlanningSuiteCase MergePolicyConservativeK6 { get; }

    public PlanningSuiteCase RequirementChainMergeRequirementLookups { get; }

    public PlanningSuiteCase RequirementDirectiveLeaksShop { get; }

    public PlanningSuiteCase RequirementSelectionMapObjectShop { get; }

    public PlanningSuiteCase RequirementPlanComplexOperation { get; }

    public PlanningSuiteCase RequirementCrossEntityAuthorInputObjectList { get; }

    public PlanningSuiteCase PlannerBehaviorMutations { get; }

    public PlanningSuiteCase WideDetailRepeatedFragmentSpreads { get; }

    public PlanningSuiteCase WideDetailSingleFragmentSpreads { get; }

    public PlanningSuiteCase InterfaceFieldOverrideSpill { get; }

    public PlanningSuiteCase DeferStepInternalPredecessor { get; }

    public PlanningSuiteCase GuardrailDeferGroups { get; }

    public PlanningSuiteCase ShareableRoutingExcludeSourceExternalProvider { get; }

    /// <summary>
    /// Gets the number of nodes the planner expanded for the benchmark method
    /// <paramref name="benchmarkName"/>, or <see langword="null"/> if there is no such case.
    /// </summary>
    public int? GetExpandedNodes(string benchmarkName)
        => _cases.TryGetValue(benchmarkName, out var planningCase)
            ? planningCase.ExpandedNodes
            : null;

    private PlanningSuiteCase Add(
        string name,
        FusionSchemaDefinition schema,
        string operationResource)
        => Add(name, schema, operationResource, OperationPlannerOptions.Default);

    private PlanningSuiteCase Add(
        string name,
        FusionSchemaDefinition schema,
        string operationResource,
        OperationPlannerOptions options)
    {
        var document = Utf8GraphQLParser.Parse(ReadResource(operationResource));
        var rewritten = new DocumentRewriter(schema).RewriteDocument(document, operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();

        var compiler = new OperationCompiler(schema, new DefaultObjectPoolProvider().CreateFieldMapPool());
        var planner = new OperationPlanner(schema, compiler, options);
        var planningCase = new PlanningSuiteCase(planner, operation);

        _cases.Add(name, planningCase);
        return planningCase;
    }

    private static FusionSchemaDefinition ComposeSchema(params string[] sourceSchemaResources)
    {
        var sourceSchemas = new List<SourceSchemaText>();
        var name = 'a';

        foreach (var resource in sourceSchemaResources)
        {
            sourceSchemas.Add(new SourceSchemaText(name.ToString(), ReadResource(resource)));
            name++;
        }

        var options = new SchemaComposerOptions { Merger = { EnableGlobalObjectIdentification = true } };
        var result = new SchemaComposer(sourceSchemas, options, new CompositionLog()).Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    private static FusionSchemaDefinition LoadSchema(string compositeSchemaResource)
        => FusionSchemaDefinition.Create(Utf8GraphQLParser.Parse(ReadResource(compositeSchemaResource)));

    private static string ReadResource(string name)
    {
        using var stream = typeof(PlanningSuiteCases).Assembly.GetManifestResourceStream($"PlanningSuite/{name}")
            ?? throw new InvalidOperationException($"The embedded resource PlanningSuite/{name} does not exist.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
