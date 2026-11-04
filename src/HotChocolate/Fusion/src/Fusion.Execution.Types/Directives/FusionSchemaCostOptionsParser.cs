using System.Globalization;
using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class FusionSchemaCostOptionsParser
{
    public static FusionSchemaCostOptions Parse(DocumentNode document, JsonDocument? settings)
    {
        var directives = document.Definitions
            .SelectMany(static definition => definition switch
            {
                SchemaDefinitionNode schemaDefinition => schemaDefinition.Directives,
                SchemaExtensionNode schemaExtension => schemaExtension.Directives,
                _ => []
            })
            .Where(static directive => directive.Name.Value.Equals(
                FusionBuiltIns.CostOptions,
                StringComparison.Ordinal))
            .Take(2)
            .ToArray();

        if (directives.Length > 1)
        {
            throw ThrowHelper.DuplicateCostOptionsDirective();
        }

        var defaultListSize = directives.Length == 0 ? null : ParseDefaultListSize(directives[0]);
        var cost = default(JsonElement);
        if (settings is not null)
        {
            var root = settings.RootElement;
            if (root.ValueKind is not JsonValueKind.Object)
            {
                throw ThrowHelper.InvalidCostSetting("$", "an object");
            }

            if (root.TryGetProperty("cost", out cost) && cost.ValueKind is not JsonValueKind.Object)
            {
                throw ThrowHelper.InvalidCostSetting("$.cost", "an object");
            }
        }

        return new FusionSchemaCostOptions(
            defaultListSize,
            ParseNumber(cost, "maxFieldCost"),
            ParseNumber(cost, "maxTypeCost"),
            ParseBoolean(cost, "enforceCostLimits"),
            ParseBoolean(cost, "skipAnalyzer"),
            ParseNumber(cost, "maxResponseSize"),
            ParseInteger(cost, "costPlanCacheSize", positive: true),
            ParseInteger(cost, "caseBudget", positive: false),
            ParseBehavior(cost));
    }

    private static int? ParseDefaultListSize(DirectiveNode directive)
    {
        var argument = directive.Arguments.FirstOrDefault(
            static argument => argument.Name.Value.Equals("defaultListSize", StringComparison.Ordinal));
        if (argument is null || argument.Value is NullValueNode)
        {
            return null;
        }

        if (argument.Value is IntValueNode intValue
            && int.TryParse(intValue.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            && value >= 0)
        {
            return value;
        }

        throw ThrowHelper.InvalidCostOptionsArgument("defaultListSize", "a non-negative integer");
    }

    private static bool TryGetProperty(JsonElement cost, string name, out JsonElement value)
    {
        if (cost.ValueKind is JsonValueKind.Object)
        {
            return cost.TryGetProperty(name, out value);
        }

        value = default;
        return false;
    }

    private static double? ParseNumber(JsonElement cost, string name)
    {
        if (!TryGetProperty(cost, name, out var element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.Number
            && element.TryGetDouble(out var value)
            && double.IsFinite(value)
            && value >= 0)
        {
            return value;
        }

        throw ThrowHelper.InvalidCostSetting($"$.cost.{name}", "a non-negative finite number");
    }

    private static bool? ParseBoolean(JsonElement cost, string name)
    {
        if (!TryGetProperty(cost, name, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw ThrowHelper.InvalidCostSetting($"$.cost.{name}", "a Boolean")
        };
    }

    private static int? ParseInteger(JsonElement cost, string name, bool positive)
    {
        if (!TryGetProperty(cost, name, out var element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.Number
            && element.TryGetInt32(out var value)
            && (!positive || value > 0))
        {
            return value;
        }

        throw ThrowHelper.InvalidCostSetting($"$.cost.{name}", positive ? "a positive integer" : "an integer");
    }

    private static FusionCaseBudgetExceededBehavior? ParseBehavior(JsonElement cost)
    {
        const string name = "caseBudgetExceededBehavior";
        if (!TryGetProperty(cost, name, out var element))
        {
            return null;
        }

        if (element.ValueKind is JsonValueKind.String)
        {
            return element.GetString() switch
            {
                nameof(FusionCaseBudgetExceededBehavior.EvaluatePerRequest) =>
                    FusionCaseBudgetExceededBehavior.EvaluatePerRequest,
                nameof(FusionCaseBudgetExceededBehavior.Overestimate) =>
                    FusionCaseBudgetExceededBehavior.Overestimate,
                _ => throw ThrowHelper.InvalidCostSetting(
                    "$.cost.caseBudgetExceededBehavior", "EvaluatePerRequest or Overestimate")
            };
        }

        throw ThrowHelper.InvalidCostSetting(
            "$.cost.caseBudgetExceededBehavior", "EvaluatePerRequest or Overestimate");
    }
}
