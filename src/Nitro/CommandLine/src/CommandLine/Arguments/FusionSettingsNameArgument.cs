namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionSettingsNameArgument : Argument<string>
{
    public const string ArgumentName = "SETTING_NAME";
    public const string AllowNonResolvableInterfaceObjects = "allow-non-resolvable-interface-objects";
    public const string CacheControlMergeBehavior = "cache-control-merge-behavior";
    public const string CaseBudget = "case-budget";
    public const string CaseBudgetExceededBehavior = "case-budget-exceeded-behavior";
    public const string CostPlanCacheSize = "cost-plan-cache-size";
    public const string DefaultListSize = "default-list-size";
    public const string EnforceCostLimits = "enforce-cost-limits";
    public const string EnumValuesMergeBehavior = "enum-values-merge-behavior";
    public const string ExcludeByTag = "exclude-by-tag";
    public const string GlobalObjectIdentification = "global-object-identification";
    public const string IncludeSatisfiabilityPaths = "include-satisfiability-paths";
    public const string MaxFieldCost = "max-field-cost";
    public const string MaxResponseSize = "max-response-size";
    public const string MaxTypeCost = "max-type-cost";
    public const string NodeResolution = "node-resolution";
    public const string ShareableFieldRuntimeTypeRouting = "shareable-field-runtime-type-routing";
    public const string SkipCostAnalyzer = "skip-cost-analyzer";
    public const string TagMergeBehavior = "tag-merge-behavior";

    public const string DepthWeight = "depth-weight";
    public const string OperationWeight = "operation-weight";
    public const string ExcessFanoutWeight = "excess-fanout-weight";
    public const string FanoutPenaltyThreshold = "fanout-penalty-threshold";
    public const string EnableRequestGrouping = "enable-request-grouping";
    public const string EnableDefer = "enable-defer";
    public const string InlineUnlabeledDeferFragments = "inline-unlabeled-defer-fragments";
    public const string MergePolicy = "merge-policy";
    public const string EnableEntityReuseSplitting = "enable-entity-reuse-splitting";
    public const string MaxPlanningTime = "max-planning-time";
    public const string MaxExpandedNodes = "max-expanded-nodes";
    public const string MaxQueueSize = "max-queue-size";
    public const string MaxGeneratedOptionsPerWorkItem = "max-generated-options-per-work-item";

    public static readonly string[] All =
    [
        AllowNonResolvableInterfaceObjects,
        CacheControlMergeBehavior,
        CaseBudget,
        CaseBudgetExceededBehavior,
        CostPlanCacheSize,
        DefaultListSize,
        EnforceCostLimits,
        EnumValuesMergeBehavior,
        ExcludeByTag,
        GlobalObjectIdentification,
        IncludeSatisfiabilityPaths,
        MaxFieldCost,
        MaxResponseSize,
        MaxTypeCost,
        NodeResolution,
        ShareableFieldRuntimeTypeRouting,
        SkipCostAnalyzer,
        TagMergeBehavior,
        DepthWeight,
        OperationWeight,
        ExcessFanoutWeight,
        FanoutPenaltyThreshold,
        EnableRequestGrouping,
        EnableDefer,
        InlineUnlabeledDeferFragments,
        MergePolicy,
        EnableEntityReuseSplitting,
        MaxPlanningTime,
        MaxExpandedNodes,
        MaxQueueSize,
        MaxGeneratedOptionsPerWorkItem
    ];

    public FusionSettingsNameArgument() : base(ArgumentName)
    {
        Description = "The name of the setting to change";
        this.AcceptOnlyFromAmong(All);
    }
}
