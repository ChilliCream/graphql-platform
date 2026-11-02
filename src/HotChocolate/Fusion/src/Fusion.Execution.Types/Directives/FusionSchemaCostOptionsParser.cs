using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class FusionSchemaCostOptionsParser
{
    public static FusionSchemaCostOptions Parse(DocumentNode document)
    {
        var costOptionsDirectives = document.Definitions
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

        if (costOptionsDirectives.Length == 0)
        {
            return FusionSchemaCostOptions.Empty;
        }

        if (costOptionsDirectives.Length > 1)
        {
            throw new InvalidOperationException(
                "The fusion__cost_options directive may only be applied once per schema.");
        }

        var costOptionsDirective = costOptionsDirectives[0];
        return new FusionSchemaCostOptions(
            ParseNonNegativeInt(costOptionsDirective, "defaultListSize"),
            ParseNonNegativeDouble(costOptionsDirective, "maxFieldCost"),
            ParseNonNegativeDouble(costOptionsDirective, "maxTypeCost"),
            ParseBoolean(costOptionsDirective, "enforceCostLimits"),
            ParseBoolean(costOptionsDirective, "skipAnalyzer"),
            ParseNonNegativeDouble(costOptionsDirective, "maxResponseSize"),
            ParsePositiveInt(costOptionsDirective, "costPlanCacheSize"),
            ParseInt(costOptionsDirective, "caseBudget"),
            ParseCaseBudgetExceededBehavior(costOptionsDirective));
    }

    private static int? ParseNonNegativeInt(DirectiveNode costOptionsDirective, string argumentName)
    {
        var argument = GetArgument(costOptionsDirective, argumentName);

        if (argument is null || argument.Value is NullValueNode)
        {
            return null;
        }

        if (argument.Value is IntValueNode intValue
            && int.TryParse(
                intValue.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value)
            && value >= 0)
        {
            return value;
        }

        throw ThrowHelper.InvalidCostOptionsArgument(argumentName, "a non-negative integer");
    }

    private static double? ParseNonNegativeDouble(DirectiveNode costOptionsDirective, string argumentName)
    {
        var argument = GetArgument(costOptionsDirective, argumentName);

        if (argument is null || argument.Value is NullValueNode)
        {
            return null;
        }

        var value = argument.Value switch
        {
            FloatValueNode floatValue => floatValue.Value,
            IntValueNode intValue => intValue.Value,
            _ => null
        };

        if (value is not null
            && double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsedValue)
            && double.IsFinite(parsedValue)
            && parsedValue >= 0)
        {
            return parsedValue;
        }

        throw ThrowHelper.InvalidCostOptionsArgument(argumentName, "a non-negative finite number");
    }

    private static bool? ParseBoolean(DirectiveNode costOptionsDirective, string argumentName)
    {
        var argument = GetArgument(costOptionsDirective, argumentName);

        if (argument is null || argument.Value is NullValueNode)
        {
            return null;
        }

        if (argument.Value is BooleanValueNode booleanValue)
        {
            return booleanValue.Value;
        }

        throw ThrowHelper.InvalidCostOptionsArgument(argumentName, "a Boolean");
    }

    private static int? ParsePositiveInt(DirectiveNode costOptionsDirective, string argumentName)
    {
        var argument = GetArgument(costOptionsDirective, argumentName);

        if (argument is null || argument.Value is NullValueNode)
        {
            return null;
        }

        if (argument.Value is IntValueNode intValue
            && int.TryParse(
                intValue.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value)
            && value > 0)
        {
            return value;
        }

        throw ThrowHelper.InvalidCostOptionsArgument(argumentName, "a positive integer");
    }

    private static int? ParseInt(DirectiveNode costOptionsDirective, string argumentName)
    {
        var argument = GetArgument(costOptionsDirective, argumentName);

        if (argument is null || argument.Value is NullValueNode)
        {
            return null;
        }

        if (argument.Value is IntValueNode intValue
            && int.TryParse(
                intValue.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var value))
        {
            return value;
        }

        throw ThrowHelper.InvalidCostOptionsArgument(argumentName, "an integer");
    }

    private static FusionCaseBudgetExceededBehavior? ParseCaseBudgetExceededBehavior(
        DirectiveNode costOptionsDirective)
    {
        const string argumentName = "caseBudgetExceededBehavior";
        var argument = GetArgument(costOptionsDirective, argumentName);

        if (argument is null || argument.Value is NullValueNode)
        {
            return null;
        }

        if (argument.Value is EnumValueNode enumValue)
        {
            return enumValue.Value switch
            {
                "EVALUATE_PER_REQUEST" => FusionCaseBudgetExceededBehavior.EvaluatePerRequest,
                "OVERESTIMATE" => FusionCaseBudgetExceededBehavior.Overestimate,
                _ => throw ThrowHelper.InvalidCostOptionsArgument(
                    argumentName,
                    "EVALUATE_PER_REQUEST or OVERESTIMATE")
            };
        }

        throw ThrowHelper.InvalidCostOptionsArgument(
            argumentName,
            "EVALUATE_PER_REQUEST or OVERESTIMATE");
    }

    private static ArgumentNode? GetArgument(DirectiveNode directive, string argumentName)
        => directive.Arguments.FirstOrDefault(
            argument => argument.Name.Value.Equals(argumentName, StringComparison.Ordinal));
}
