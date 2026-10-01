using System.Collections.Immutable;
using System.Globalization;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering.Nodes;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Filtering;

internal static class FilterCompiler
{
    public static OpenTelemetryFilterInput? Compile(
        string? filter,
        string freeTextKey,
        TelemetryFilterSignal signal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(freeTextKey);

        return string.IsNullOrWhiteSpace(filter)
            ? null
            : Compile(FilterParser.Parse(filter, signal), freeTextKey);
    }

    public static OpenTelemetryFilterInput? Compile(FilterNode? node, string freeTextKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(freeTextKey);

        return node switch
        {
            null => null,
            FilterAndNode and => and.Children.Combine(freeTextKey, isAnd: true),
            FilterOrNode or => or.Children.Combine(freeTextKey, isAnd: false),
            FilterNotNode not => not.CompileNot(freeTextKey),
            FilterTermNode term when term.Text.Length == 0 => null,
            FilterTermNode term => OpenTelemetryFilterInput.FromAttribute(freeTextKey, new OpenTelemetryAttributeConditionInput
            {
                Matches = term.Text.ToContainsPattern()
            }),
            FilterPredicateNode predicate => predicate.CompilePredicate(),
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }

    internal static OpenTelemetryFilterInput FreeText(string text, string freeTextKey)
        => OpenTelemetryFilterInput.FromAttribute(freeTextKey, new OpenTelemetryAttributeConditionInput
        {
            Matches = text.ToContainsPattern()
        });
}

file static class Extensions
{
    private static readonly (string Prefix, OpenTelemetryAttributeKind Kind)[] s_scopeKinds =
    [
        ("@span.", OpenTelemetryAttributeKind.Span),
        ("@log.", OpenTelemetryAttributeKind.Log),
        ("@resource.", OpenTelemetryAttributeKind.Resource),
        ("@event.", OpenTelemetryAttributeKind.Event),
        ("@body.", OpenTelemetryAttributeKind.Body)
    ];

    extension(OpenTelemetryFilterInput)
    {
        public static OpenTelemetryFilterInput FromAttribute(
            string field,
            OpenTelemetryAttributeConditionInput condition)
        {
            var (key, kind) = field.LowerKey();
            var predicate = new OpenTelemetryAttributePredicateInput
            {
                Key = key,
                Condition = condition
            };
            if (kind is not null)
            {
                predicate = predicate with { Kind = kind };
            }

            return new OpenTelemetryFilterInput
            {
                Attribute = predicate
            };
        }
    }

    extension(IReadOnlyList<FilterNode> children)
    {
        public OpenTelemetryFilterInput? Combine(string freeTextKey, bool isAnd)
        {
            var inputs = children
                .Select(child => FilterCompiler.Compile(child, freeTextKey))
                .OfType<OpenTelemetryFilterInput>()
                .ToArray();

            return inputs.Length switch
            {
                0 => null,
                1 => inputs[0],
                _ when isAnd => new OpenTelemetryFilterInput { And = inputs },
                _ => new OpenTelemetryFilterInput { Or = inputs }
            };
        }
    }

    extension(FilterNotNode node)
    {
        public OpenTelemetryFilterInput? CompileNot(string freeTextKey)
        {
            if (node.Child is FilterPredicateNode { Operator: FilterComparisonOperator.Exists } predicate)
            {
                return OpenTelemetryFilterInput.FromAttribute(
                    predicate.Field,
                    new OpenTelemetryAttributeConditionInput { Exists = false });
            }

            var child = FilterCompiler.Compile(node.Child, freeTextKey);
            return child is null ? null : new OpenTelemetryFilterInput { Not = child };
        }
    }

    extension(FilterPredicateNode node)
    {
        public OpenTelemetryFilterInput CompilePredicate()
        {
            if (node.Operator == FilterComparisonOperator.Range)
            {
                return new OpenTelemetryFilterInput
                {
                    And =
                    [
                        OpenTelemetryFilterInput.FromAttribute(
                            node.Field,
                            new OpenTelemetryAttributeConditionInput { Gte = node.Values[0].ToScalar() }),
                        OpenTelemetryFilterInput.FromAttribute(
                            node.Field,
                            new OpenTelemetryAttributeConditionInput { Lte = node.Values[1].ToScalar() })
                    ]
                };
            }

            if (node.Operator == FilterComparisonOperator.In)
            {
                return node.CompileSet();
            }

            return OpenTelemetryFilterInput.FromAttribute(node.Field, node.Operator.ToCondition(node.Values));
        }

        public OpenTelemetryFilterInput CompileSet()
        {
            var patterns = node.Values.Where(static value => value.HasWildcard).ToArray();
            if (patterns.Length == 0)
            {
                return OpenTelemetryFilterInput.FromAttribute(node.Field, new OpenTelemetryAttributeConditionInput
                {
                    In = node.Values.Select(static value => value.ToScalar()).ToArray()
                });
            }

            var branches = new List<OpenTelemetryFilterInput>();
            var literals = node.Values.Where(static value => !value.HasWildcard).ToArray();
            if (literals.Length > 0)
            {
                branches.Add(OpenTelemetryFilterInput.FromAttribute(node.Field, new OpenTelemetryAttributeConditionInput
                {
                    In = literals.Select(static value => value.ToScalar()).ToArray()
                }));
            }

            branches.AddRange(patterns.Select(value => OpenTelemetryFilterInput.FromAttribute(
                node.Field,
                new OpenTelemetryAttributeConditionInput { Matches = value.Text })));

            return branches.Count == 1
                ? branches[0]
                : new OpenTelemetryFilterInput { Or = branches };
        }
    }

    extension(FilterComparisonOperator comparison)
    {
        public OpenTelemetryAttributeConditionInput ToCondition(ImmutableArray<FilterValue> values)
        {
            var value = values.Length > 0 ? values[0] : default;
            return comparison switch
            {
                FilterComparisonOperator.Equal when value.HasWildcard => new OpenTelemetryAttributeConditionInput
                {
                    Matches = value.Text
                },
                FilterComparisonOperator.Equal => new OpenTelemetryAttributeConditionInput { Eq = value.ToScalar() },
                FilterComparisonOperator.GreaterThan => new OpenTelemetryAttributeConditionInput { Gt = value.ToScalar() },
                FilterComparisonOperator.GreaterThanOrEqual => new OpenTelemetryAttributeConditionInput { Gte = value.ToScalar() },
                FilterComparisonOperator.LessThan => new OpenTelemetryAttributeConditionInput { Lt = value.ToScalar() },
                FilterComparisonOperator.LessThanOrEqual => new OpenTelemetryAttributeConditionInput { Lte = value.ToScalar() },
                FilterComparisonOperator.Exists => new OpenTelemetryAttributeConditionInput { Exists = true },
                _ => throw new ArgumentOutOfRangeException(nameof(comparison))
            };
        }
    }

    extension(FilterValue value)
    {
        public OpenTelemetryAttributeValueInput ToScalar()
        {
            return value.Kind switch
            {
                FilterValueKind.Number when
                    !value.Text.Contains('.')
                    &&
                    int.TryParse(value.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer) =>
                    new OpenTelemetryAttributeValueInput { Int = integer },
                FilterValueKind.Number => new OpenTelemetryAttributeValueInput
                {
                    Float = double.Parse(value.Text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)
                },
                FilterValueKind.Boolean => new OpenTelemetryAttributeValueInput { Boolean = value.Text == "true" },
                _ => new OpenTelemetryAttributeValueInput { String = value.Text }
            };
        }
    }

    extension(string text)
    {
        public (string Key, OpenTelemetryAttributeKind? Kind) LowerKey()
        {
            foreach (var (prefix, kind) in s_scopeKinds)
            {
                if (text.StartsWith(prefix, StringComparison.Ordinal) && text.Length > prefix.Length)
                {
                    return (text[prefix.Length..], kind);
                }
            }

            return (text, null);
        }

        public string ToContainsPattern()
        {
            var start = text.StartsWith('*') ? string.Empty : "*";
            var end = text.Length > 1 && text.EndsWith('*') ? string.Empty : "*";
            return start + text + end;
        }
    }
}
