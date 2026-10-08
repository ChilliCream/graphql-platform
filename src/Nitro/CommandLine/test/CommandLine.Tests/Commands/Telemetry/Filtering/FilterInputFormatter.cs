using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Filtering;

internal static class FilterInputFormatter
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(OpenTelemetryFilterInput? input)
        => JsonSerializer.Serialize(input, s_serializerOptions);

    public static string Describe(OpenTelemetryFilterInput? input)
    {
        if (input is null)
        {
            return "null";
        }

        if (input.And is not null)
        {
            return $"and({string.Join(',', input.And.Select(Describe))})";
        }

        if (input.Or is not null)
        {
            return $"or({string.Join(',', input.Or.Select(Describe))})";
        }

        if (input.Not is not null)
        {
            return $"not({Describe(input.Not)})";
        }

        if (input.Attribute is not null)
        {
            var kind = input.Attribute.Kind is null ? string.Empty : $"@{input.Attribute.Kind}";
            return $"attribute({input.Attribute.Key}{kind},{Describe(input.Attribute.Condition)})";
        }

        return "invalid";
    }

    private static string Describe(OpenTelemetryAttributeConditionInput condition)
    {
        if (condition.Eq is not null)
        {
            return $"eq({Describe(condition.Eq)})";
        }

        if (condition.Gt is not null)
        {
            return $"gt({Describe(condition.Gt)})";
        }

        if (condition.Gte is not null)
        {
            return $"gte({Describe(condition.Gte)})";
        }

        if (condition.Lt is not null)
        {
            return $"lt({Describe(condition.Lt)})";
        }

        if (condition.Lte is not null)
        {
            return $"lte({Describe(condition.Lte)})";
        }

        if (condition.In is not null)
        {
            return $"in({string.Join(',', condition.In.Select(Describe))})";
        }

        if (condition.Matches is not null)
        {
            return $"matches({condition.Matches})";
        }

        return $"exists({condition.Exists})";
    }

    private static string Describe(OpenTelemetryAttributeValueInput value)
    {
        if (value.String is not null)
        {
            return $"string:{value.String}";
        }

        if (value.Int is not null)
        {
            return $"int:{value.Int}";
        }

        if (value.Float is not null)
        {
            return $"float:{value.Float:0.################}";
        }

        return $"boolean:{value.Boolean!.Value.ToString().ToLowerInvariant()}";
    }
}
