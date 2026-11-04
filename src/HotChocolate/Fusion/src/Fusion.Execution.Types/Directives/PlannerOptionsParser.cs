using System.Globalization;
using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class PlannerOptionsParser
{
    public static PlannerOptions Parse(DocumentNode document, JsonDocument? settings)
    {
        var executionDirectives = document.Definitions
            .SelectMany(static definition => definition switch
            {
                SchemaDefinitionNode schemaDefinition => schemaDefinition.Directives,
                SchemaExtensionNode schemaExtension => schemaExtension.Directives,
                _ => []
            })
            .Where(static directive => directive.Name.Value.Equals(
                FusionBuiltIns.Execution,
                StringComparison.Ordinal))
            .Take(2)
            .ToArray();

        if (executionDirectives.Length > 1)
        {
            throw ThrowHelper.DuplicateExecutionDirective();
        }

        var planner = default(JsonElement);
        if (settings is not null)
        {
            var root = settings.RootElement;
            if (root.ValueKind is not JsonValueKind.Object)
            {
                throw ThrowHelper.InvalidPlannerSetting("$", "an object");
            }

            if (root.TryGetProperty("planner", out planner) && planner.ValueKind is not JsonValueKind.Object)
            {
                throw ThrowHelper.InvalidPlannerSetting("$.planner", "an object");
            }
        }

        return new PlannerOptions(
            executionDirectives.Length == 0 ? NodeResolution.Gateway : ParseNodeResolution(executionDirectives[0]),
            executionDirectives.Length == 0 ? ShareableFieldRuntimeTypeRouting.SourceLocal : ParseShareableFieldRuntimeTypeRouting(executionDirectives[0]))
        {
            DepthWeight = ParseNumber(planner, "depthWeight"),
            OperationWeight = ParseNumber(planner, "operationWeight"),
            EnableRequestGrouping = ParseBoolean(planner, "enableRequestGrouping"),
            EnableDefer = ParseBoolean(planner, "enableDefer"),
            InlineUnlabeledDeferFragments = ParseBoolean(planner, "inlineUnlabeledDeferFragments"),
            MergePolicy = ParseMergePolicy(planner, "mergePolicy"),
            EnableEntityReuseSplitting = ParseBoolean(planner, "enableEntityReuseSplitting"),
            ExcessFanoutWeight = ParseNumber(planner, "excessFanoutWeight"),
            FanoutPenaltyThreshold = ParseInteger(planner, "fanoutPenaltyThreshold", positive: false),
            MaxPlanningTime = ParseTime(planner, "maxPlanningTime"),
            MaxExpandedNodes = ParseInteger(planner, "maxExpandedNodes", positive: true),
            MaxQueueSize = ParseInteger(planner, "maxQueueSize", positive: true),
            MaxGeneratedOptionsPerWorkItem = ParseInteger(planner, "maxGeneratedOptionsPerWorkItem", positive: true)
        };
    }

    private static NodeResolution ParseNodeResolution(DirectiveNode executionDirective)
    {
        var nodeResolutionArgument = executionDirective.Arguments.FirstOrDefault(
            static argument => argument.Name.Value.Equals(
                "nodeResolution",
                StringComparison.Ordinal));

        if (nodeResolutionArgument is null)
        {
            return NodeResolution.Gateway;
        }

        return nodeResolutionArgument.Value switch
        {
            EnumValueNode { Value: "GATEWAY" } => NodeResolution.Gateway,
            EnumValueNode { Value: "SOURCE_SCHEMA" } => NodeResolution.SourceSchema,
            _ => throw ThrowHelper.InvalidExecutionArgument("nodeResolution", "GATEWAY or SOURCE_SCHEMA")
        };
    }

    private static ShareableFieldRuntimeTypeRouting ParseShareableFieldRuntimeTypeRouting(
        DirectiveNode executionDirective)
    {
        var argument = executionDirective.Arguments.FirstOrDefault(
            static argument => argument.Name.Value.Equals(
                "shareableFieldRuntimeTypeRouting",
                StringComparison.Ordinal));

        if (argument is null)
        {
            return ShareableFieldRuntimeTypeRouting.SourceLocal;
        }

        return argument.Value switch
        {
            EnumValueNode { Value: "SOURCE_LOCAL" } =>
                ShareableFieldRuntimeTypeRouting.SourceLocal,
            EnumValueNode { Value: "COMMON_RUNTIME_TYPES" } =>
                ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes,
            _ => throw ThrowHelper.InvalidExecutionArgument(
                "shareableFieldRuntimeTypeRouting", "SOURCE_LOCAL or COMMON_RUNTIME_TYPES")
        };
    }

    private static bool TryGetProperty(JsonElement planner, string name, out JsonElement value)
    {
        if (planner.ValueKind is JsonValueKind.Object)
        {
            return planner.TryGetProperty(name, out value);
        }

        value = default;
        return false;
    }

    private static double? ParseNumber(JsonElement planner, string name)
    {
        if (!TryGetProperty(planner, name, out var element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.Number && element.TryGetDouble(out var value) && double.IsFinite(value))
        {
            return value;
        }

        throw ThrowHelper.InvalidPlannerSetting($"$.planner.{name}", "a finite number");
    }

    private static bool? ParseBoolean(JsonElement planner, string name)
    {
        if (!TryGetProperty(planner, name, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw ThrowHelper.InvalidPlannerSetting($"$.planner.{name}", "a Boolean")
        };
    }

    private static int? ParseInteger(JsonElement planner, string name, bool positive)
    {
        if (!TryGetProperty(planner, name, out var element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.Number && element.TryGetInt32(out var value) && (!positive || value > 0))
        {
            return value;
        }

        throw ThrowHelper.InvalidPlannerSetting($"$.planner.{name}", positive ? "a positive integer" : "an integer");
    }

    private static TimeSpan? ParseTime(JsonElement planner, string name)
    {
        if (!TryGetProperty(planner, name, out var element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.String
            && TimeSpan.TryParseExact(element.GetString(), "c", CultureInfo.InvariantCulture, out var value)
            && value > TimeSpan.Zero)
        {
            return value;
        }

        throw ThrowHelper.InvalidPlannerSetting($"$.planner.{name}", "a positive TimeSpan in c format");
    }

    private static FusionOperationMergePolicy? ParseMergePolicy(JsonElement planner, string name)
    {
        if (!TryGetProperty(planner, name, out var element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.String)
        {
            return element.GetString() switch
            {
                nameof(FusionOperationMergePolicy.None) => FusionOperationMergePolicy.None,
                nameof(FusionOperationMergePolicy.Conservative) => FusionOperationMergePolicy.Conservative,
                nameof(FusionOperationMergePolicy.Balanced) => FusionOperationMergePolicy.Balanced,
                nameof(FusionOperationMergePolicy.Aggressive) => FusionOperationMergePolicy.Aggressive,
                _ => throw ThrowHelper.InvalidPlannerSetting($"$.planner.{name}", "None, Conservative, Balanced or Aggressive")
            };
        }

        throw ThrowHelper.InvalidPlannerSetting($"$.planner.{name}", "None, Conservative, Balanced or Aggressive");
    }
}
