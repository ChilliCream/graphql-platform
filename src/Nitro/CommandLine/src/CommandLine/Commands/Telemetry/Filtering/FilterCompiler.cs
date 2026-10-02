using System.Collections.Immutable;
using System.Globalization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class FilterCompiler
{
    public static OpenTelemetryFilterInput? Compile(string? filter, string freeTextKey, TelemetryFilterSignal signal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(freeTextKey);

        return string.IsNullOrWhiteSpace(filter) ? null : Compile(FilterParser.Parse(filter, signal), freeTextKey);
    }

    public static OpenTelemetryFilterInput? Compile(FilterNode? node, string freeTextKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(freeTextKey);

        return node switch
        {
            null => null,
            FilterAndNode and => Compile(and, freeTextKey),
            FilterOrNode or => Compile(or, freeTextKey),
            FilterNotNode not => Compile(not, freeTextKey),
            FilterTermNode term => Compile(term, freeTextKey),
            FilterPredicateNode predicate => Compile(predicate),
            _ => throw ThrowHelper.UnsupportedFilterNode(node)
        };
    }

    internal static OpenTelemetryFilterInput FreeText(string text, string freeTextKey)
        => Attribute(freeTextKey, new OpenTelemetryAttributeConditionInput { Matches = ContainsPattern(text) });

    internal static OpenTelemetryFilterInput Attribute(string field, OpenTelemetryAttributeConditionInput condition)
    {
        var (key, kind) = SplitScope(field);
        var predicate = new OpenTelemetryAttributePredicateInput { Key = key, Condition = condition };
        if (kind is not null)
        {
            predicate = predicate with { Kind = kind };
        }

        return new OpenTelemetryFilterInput { Attribute = predicate };
    }

    internal static (string Key, OpenTelemetryAttributeKind? Kind) SplitScope(string field)
    {
        foreach (var (prefix, kind) in s_scopeKinds)
        {
            if (field.StartsWith(prefix, StringComparison.Ordinal)
                && field.Length > prefix.Length)
            {
                return (field[prefix.Length..], kind);
            }
        }

        return (field, null);
    }

    private static OpenTelemetryFilterInput? Compile(FilterAndNode node, string freeTextKey)
    {
        var children = CompileChildren(node.Children, freeTextKey);

        return children.Length switch
        {
            0 => null,
            1 => children[0],
            _ => new OpenTelemetryFilterInput { And = children }
        };
    }

    private static OpenTelemetryFilterInput? Compile(FilterOrNode node, string freeTextKey)
    {
        var children = CompileChildren(node.Children, freeTextKey);

        return children.Length switch
        {
            0 => null,
            1 => children[0],
            _ => new OpenTelemetryFilterInput { Or = children }
        };
    }

    private static OpenTelemetryFilterInput? Compile(FilterNotNode node, string freeTextKey)
    {
        if (node.Child is FilterPredicateNode { Operator: FilterComparisonOperator.Exists } predicate)
        {
            return Attribute(predicate.Field, new OpenTelemetryAttributeConditionInput { Exists = false });
        }

        var child = Compile(node.Child, freeTextKey);
        return child is null ? null : new OpenTelemetryFilterInput { Not = child };
    }

    private static OpenTelemetryFilterInput? Compile(FilterTermNode node, string freeTextKey)
        => node.Text.Length == 0 ? null : FreeText(node.Text, freeTextKey);

    private static OpenTelemetryFilterInput Compile(FilterPredicateNode node)
    {
        if (node.Operator == FilterComparisonOperator.Range)
        {
            return new OpenTelemetryFilterInput
            {
                And =
                [
                    Attribute(node.Field, new OpenTelemetryAttributeConditionInput { Gte = Scalar(node.Values[0]) }),
                    Attribute(node.Field, new OpenTelemetryAttributeConditionInput { Lte = Scalar(node.Values[1]) })
                ]
            };
        }

        if (node.Operator == FilterComparisonOperator.In)
        {
            return CompileSet(node);
        }

        return Attribute(node.Field, Condition(node.Operator, node.Values));
    }

    private static OpenTelemetryFilterInput[] CompileChildren(IReadOnlyList<FilterNode> children, string freeTextKey)
        => children.Select(child => Compile(child, freeTextKey)).OfType<OpenTelemetryFilterInput>().ToArray();

    private static OpenTelemetryFilterInput CompileSet(FilterPredicateNode node)
    {
        var patterns = node.Values.Where(static value => value.HasWildcard).ToArray();
        if (patterns.Length == 0)
        {
            return Attribute(
                node.Field,
                new OpenTelemetryAttributeConditionInput { In = node.Values.Select(Scalar).ToArray() });
        }

        var branches = new List<OpenTelemetryFilterInput>();
        var literals = node.Values.Where(static value => !value.HasWildcard).ToArray();
        if (literals.Length > 0)
        {
            branches.Add(
                Attribute(
                    node.Field,
                    new OpenTelemetryAttributeConditionInput { In = literals.Select(Scalar).ToArray() }));
        }

        branches.AddRange(
            patterns.Select(value =>
                Attribute(node.Field, new OpenTelemetryAttributeConditionInput { Matches = value.Text })
            ));

        return branches.Count == 1 ? branches[0] : new OpenTelemetryFilterInput { Or = branches };
    }

    private static OpenTelemetryAttributeConditionInput Condition(
        FilterComparisonOperator comparison,
        ImmutableArray<FilterValue> values)
    {
        var value = values.Length > 0 ? values[0] : default;
        return comparison switch
        {
            FilterComparisonOperator.Equal when value.HasWildcard => new OpenTelemetryAttributeConditionInput
            {
                Matches = value.Text
            },
            FilterComparisonOperator.Equal => new OpenTelemetryAttributeConditionInput { Eq = Scalar(value) },
            FilterComparisonOperator.GreaterThan => new OpenTelemetryAttributeConditionInput { Gt = Scalar(value) },
            FilterComparisonOperator.GreaterThanOrEqual => new OpenTelemetryAttributeConditionInput
            {
                Gte = Scalar(value)
            },
            FilterComparisonOperator.LessThan => new OpenTelemetryAttributeConditionInput { Lt = Scalar(value) },
            FilterComparisonOperator.LessThanOrEqual => new OpenTelemetryAttributeConditionInput
            {
                Lte = Scalar(value)
            },
            FilterComparisonOperator.Exists => new OpenTelemetryAttributeConditionInput { Exists = true },
            _ => throw ThrowHelper.UnsupportedFilterComparisonOperator(comparison)
        };
    }

    private static OpenTelemetryAttributeValueInput Scalar(FilterValue value)
    {
        return value.Kind switch
        {
            FilterValueKind.Number
                when !value.Text.Contains('.')
                    && int.TryParse(
                        value.Text,
                        NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture,
                        out var integer) => new OpenTelemetryAttributeValueInput { Int = integer },
            FilterValueKind.Number => new OpenTelemetryAttributeValueInput
            {
                Float = double.Parse(
                    value.Text,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture)
            },
            FilterValueKind.Boolean => new OpenTelemetryAttributeValueInput { Boolean = value.Text == "true" },
            _ => new OpenTelemetryAttributeValueInput { String = value.Text }
        };
    }

    private static string ContainsPattern(string text)
    {
        var start = text.StartsWith('*') ? string.Empty : "*";
        var end = text.Length > 1 && text.EndsWith('*') ? string.Empty : "*";
        return start + text + end;
    }

    private static readonly (string Prefix, OpenTelemetryAttributeKind Kind)[] s_scopeKinds =
    [
        ("@span.", OpenTelemetryAttributeKind.Span),
        ("@log.", OpenTelemetryAttributeKind.Log),
        ("@resource.", OpenTelemetryAttributeKind.Resource),
        ("@event.", OpenTelemetryAttributeKind.Event),
        ("@body.", OpenTelemetryAttributeKind.Body)
    ];
}
