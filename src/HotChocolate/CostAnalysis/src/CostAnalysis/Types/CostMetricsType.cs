using System.Text.RegularExpressions;
using HotChocolate.Types;
using static HotChocolate.CostAnalysis.WellKnownArgumentNames;

namespace HotChocolate.CostAnalysis.Types;

internal sealed class CostMetricsType : ObjectType<CostMetrics>
{
    protected override void Configure(IObjectTypeDescriptor<CostMetrics> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor
            .Field(c => c.FieldCounts)
            .Argument(RegexName, d => d.Type<StringType>())
            .Resolve(context => ToCostCountTypes(
                context.Parent<CostMetrics>().FieldCounts,
                context.ArgumentValue<string?>(RegexName)));

        descriptor
            .Field(c => c.TypeCounts)
            .Argument(RegexName, d => d.Type<StringType>())
            .Resolve(context => ToCostCountTypes(
                context.Parent<CostMetrics>().TypeCounts,
                context.ArgumentValue<string?>(RegexName)));

        descriptor
            .Field(c => c.InputTypeCounts)
            .Argument(RegexName, d => d.Type<StringType>())
            .Resolve(context => ToCostCountTypes(
                context.Parent<CostMetrics>().InputTypeCounts,
                context.ArgumentValue<string?>(RegexName)));

        descriptor
            .Field(c => c.InputFieldCounts)
            .Argument(RegexName, d => d.Type<StringType>())
            .Resolve(context => ToCostCountTypes(
                context.Parent<CostMetrics>().InputFieldCounts,
                context.ArgumentValue<string?>(RegexName)));

        descriptor
            .Field(c => c.ArgumentCounts)
            .Argument(RegexName, d => d.Type<StringType>())
            .Resolve(context => ToCostCountTypes(
                context.Parent<CostMetrics>().ArgumentCounts,
                context.ArgumentValue<string?>(RegexName)));

        descriptor
            .Field(c => c.DirectiveCounts)
            .Argument(RegexName, d => d.Type<StringType>())
            .Resolve(context => ToCostCountTypes(
                context.Parent<CostMetrics>().DirectiveCounts,
                context.ArgumentValue<string?>(RegexName)));

        descriptor
            .Field(c => c.FieldCost);

        descriptor
            .Field(c => c.TypeCost);

        descriptor
            .Field(c => c.FieldCostByLocation)
            .Argument(RegexPath, d => d.Type<StringType>())
            .Resolve(context => ToCostsByLocation(
                context.Parent<CostMetrics>().FieldCostByLocation,
                context.ArgumentValue<string?>(RegexPath)));

        descriptor
            .Field(c => c.TypeCostByLocation)
            .Argument(RegexPath, d => d.Type<StringType>())
            .Resolve(context => ToCostsByLocation(
                context.Parent<CostMetrics>().TypeCostByLocation,
                context.ArgumentValue<string?>(RegexPath)));
    }

    private static IEnumerable<CostCountType> ToCostCountTypes(
        Dictionary<string, int> dictionary,
        string? regexName)
    {
        var regex = regexName is null ? null : CreateRegex(regexName);

        foreach (var (key, value) in dictionary)
        {
            if (regex?.IsMatch(key) == false)
            {
                continue;
            }

            yield return new CostCountType(key, value);
        }
    }

    private static IEnumerable<CostByLocation> ToCostsByLocation(
        Dictionary<string, double> dictionary,
        string? regexPath)
    {
        var regex = regexPath is null ? null : CreateRegex(regexPath);

        foreach (var (key, value) in dictionary)
        {
            if (regex?.IsMatch(key) == false)
            {
                continue;
            }

            yield return new CostByLocation(key, value);
        }
    }

    private static Regex CreateRegex(string regexString)
    {
        // This regular expression always applies to the full name/path, as if the start string and
        // end string qualifiers are always specified.
        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-regexName-Arguments
        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-regexPath-Arguments
        regexString = "^" + regexString.TrimStart('^').TrimEnd('$') + "$";

        var regex = new Regex(regexString, RegexOptions.Compiled | RegexOptions.NonBacktracking);

        return regex;
    }
}
