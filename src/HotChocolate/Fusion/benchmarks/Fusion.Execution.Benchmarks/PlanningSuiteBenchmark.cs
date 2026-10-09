using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Measures operation planning time for the Shop and K6 planner cases and the slowest
/// planning tests, one benchmark per case, each with default planner options unless the
/// test sets its own. Numbers are compared per case, with one process per case selected by
/// <c>--filter "*PlanningSuiteBenchmark.&lt;Case&gt;"</c>.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(PlanningSuiteConfig))]
[MarkdownExporter]
public class PlanningSuiteBenchmark
{
    private PlanningSuiteCases _cases = null!;

    [GlobalSetup]
    public void GlobalSetup() => _cases = PlanningSuiteCases.Instance;

    [Benchmark]
    public int Shop_Medium_Query_With_Aliases()
        => _cases.ShopMediumQueryWithAliases.Plan().SearchSpace;

    [Benchmark]
    public int K6_DeepNesting()
        => _cases.K6DeepNesting.Plan().SearchSpace;

    [Benchmark]
    public int MergePolicy_Aggressive_K6()
        => _cases.MergePolicyAggressiveK6.Plan().SearchSpace;

    [Benchmark]
    public int MergePolicy_Balanced_K6()
        => _cases.MergePolicyBalancedK6.Plan().SearchSpace;

    [Benchmark]
    public int MergePolicy_Conservative_K6()
        => _cases.MergePolicyConservativeK6.Plan().SearchSpace;

    [Benchmark]
    public int RequirementChain_Merge_Requirement_Lookups()
        => _cases.RequirementChainMergeRequirementLookups.Plan().SearchSpace;

    [Benchmark]
    public int Requirement_Directive_Leaks_Shop()
        => _cases.RequirementDirectiveLeaksShop.Plan().SearchSpace;

    [Benchmark]
    public int Requirement_SelectionMap_Object_Shop()
        => _cases.RequirementSelectionMapObjectShop.Plan().SearchSpace;

    [Benchmark]
    public int Requirement_Plan_Complex_Operation()
        => _cases.RequirementPlanComplexOperation.Plan().SearchSpace;

    [Benchmark]
    public int RequirementCrossEntity_Author_Input_Object_List()
        => _cases.RequirementCrossEntityAuthorInputObjectList.Plan().SearchSpace;

    [Benchmark]
    public int PlannerBehavior_Mutations()
        => _cases.PlannerBehaviorMutations.Plan().SearchSpace;

    [Benchmark]
    public int WideDetail_Repeated_Fragment_Spreads()
        => _cases.WideDetailRepeatedFragmentSpreads.Plan().SearchSpace;

    [Benchmark]
    public int WideDetail_Single_Fragment_Spreads()
        => _cases.WideDetailSingleFragmentSpreads.Plan().SearchSpace;

    [Benchmark]
    public int InterfaceFieldOverride_Spill()
        => _cases.InterfaceFieldOverrideSpill.Plan().SearchSpace;

    [Benchmark]
    public int Defer_Step_Internal_Predecessor()
        => _cases.DeferStepInternalPredecessor.Plan().SearchSpace;

    [Benchmark]
    public int Guardrail_Defer_Groups()
        => _cases.GuardrailDeferGroups.Plan().SearchSpace;

    [Benchmark]
    public int ShareableRouting_Exclude_Source_External_Provider()
        => _cases.ShareableRoutingExcludeSourceExternalProvider.Plan().SearchSpace;
}

/// <summary>
/// Runs the default job in process like <see cref="InProcessDefaultRunConfig"/> and adds the
/// expanded node count of each case to the summary.
/// </summary>
public sealed class PlanningSuiteConfig : InProcessDefaultRunConfig
{
    public PlanningSuiteConfig()
    {
        AddColumn(new ExpandedNodesColumn());
    }
}

/// <summary>
/// Reports how many nodes the planner expanded for a <see cref="PlanningSuiteBenchmark"/> case.
/// </summary>
public sealed class ExpandedNodesColumn : IColumn
{
    public string Id => nameof(ExpandedNodesColumn);

    public string ColumnName => "expandedNodes";

    public string Legend => "Number of nodes the planner expanded to plan the operation";

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Custom;

    public int PriorityInCategory => 0;

    public bool IsNumeric => true;

    public UnitType UnitType => UnitType.Dimensionless;

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        if (!PlanningSuiteCases.IsCreated)
        {
            return "?";
        }

        var expandedNodes = PlanningSuiteCases.Instance.GetExpandedNodes(benchmarkCase.Descriptor.WorkloadMethod.Name);
        return expandedNodes?.ToString() ?? "?";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        => GetValue(summary, benchmarkCase);
}
