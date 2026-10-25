using System.Collections.Immutable;
using System.Globalization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class FilterCompiler
{
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

    internal static OpenTelemetryFilterInput CreateFreeTextFilter(string text, string freeTextKey)
        => CreateAttributeFilter(
            freeTextKey,
            new OpenTelemetryAttributeConditionInput { Matches = CreateContainsPattern(text) });

    internal static OpenTelemetryFilterInput CreateAttributeFilter(
        string field,
        OpenTelemetryAttributeConditionInput condition)
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
            return CreateAttributeFilter(predicate.Field, new OpenTelemetryAttributeConditionInput { Exists = false });
        }

        var child = Compile(node.Child, freeTextKey);
        return child is null ? null : new OpenTelemetryFilterInput { Not = child };
    }

    private static OpenTelemetryFilterInput? Compile(FilterTermNode node, string freeTextKey)
        => node.Text.Length == 0 ? null : CreateFreeTextFilter(node.Text, freeTextKey);

    private static OpenTelemetryFilterInput Compile(FilterPredicateNode node)
    {
        if (node.Operator == FilterComparisonOperator.Range)
        {
            return new OpenTelemetryFilterInput
            {
                And =
                [
                    CreateAttributeFilter(
                        node.Field,
                        new OpenTelemetryAttributeConditionInput { Gte = CreateScalar(node.Values[0]) }),
                    CreateAttributeFilter(
                        node.Field,
                        new OpenTelemetryAttributeConditionInput { Lte = CreateScalar(node.Values[1]) })
                ]
            };
        }

        if (node.Operator == FilterComparisonOperator.In)
        {
            return CompileSet(node);
        }

        return CreateAttributeFilter(node.Field, CreateCondition(node.Operator, node.Values));
    }

    private static OpenTelemetryFilterInput[] CompileChildren(IReadOnlyList<FilterNode> children, string freeTextKey)
        => children.Select(child => Compile(child, freeTextKey)).OfType<OpenTelemetryFilterInput>().ToArray();

    private static OpenTelemetryFilterInput CompileSet(FilterPredicateNode node)
    {
        var patterns = node.Values.Where(static value => value.HasWildcard).ToArray();
        if (patterns.Length == 0)
        {
            return CreateAttributeFilter(
                node.Field,
                new OpenTelemetryAttributeConditionInput { In = node.Values.Select(CreateScalar).ToArray() });
        }

        var branches = new List<OpenTelemetryFilterInput>();
        var literals = node.Values.Where(static value => !value.HasWildcard).ToArray();
        if (literals.Length > 0)
        {
            branches.Add(
                CreateAttributeFilter(
                    node.Field,
                    new OpenTelemetryAttributeConditionInput { In = literals.Select(CreateScalar).ToArray() }));
        }

        branches.AddRange(
            patterns.Select(value =>
                CreateAttributeFilter(node.Field, new OpenTelemetryAttributeConditionInput { Matches = value.Text })
            ));

        return branches.Count == 1 ? branches[0] : new OpenTelemetryFilterInput { Or = branches };
    }

    private static OpenTelemetryAttributeConditionInput CreateCondition(
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
            FilterComparisonOperator.Equal => new OpenTelemetryAttributeConditionInput { Eq = CreateScalar(value) },
            FilterComparisonOperator.GreaterThan => new OpenTelemetryAttributeConditionInput
            {
                Gt = CreateScalar(value)
            },
            FilterComparisonOperator.GreaterThanOrEqual => new OpenTelemetryAttributeConditionInput
            {
                Gte = CreateScalar(value)
            },
            FilterComparisonOperator.LessThan => new OpenTelemetryAttributeConditionInput { Lt = CreateScalar(value) },
            FilterComparisonOperator.LessThanOrEqual => new OpenTelemetryAttributeConditionInput
            {
                Lte = CreateScalar(value)
            },
            FilterComparisonOperator.Exists => new OpenTelemetryAttributeConditionInput { Exists = true },
            _ => throw ThrowHelper.UnsupportedFilterComparisonOperator(comparison)
        };
    }

    private static OpenTelemetryAttributeValueInput CreateScalar(FilterValue value)
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

    private static string CreateContainsPattern(string text)
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
