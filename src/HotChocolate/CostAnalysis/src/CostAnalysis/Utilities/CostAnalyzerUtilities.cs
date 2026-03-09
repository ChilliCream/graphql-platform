using HotChocolate.CostAnalysis.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Utilities;

internal static class CostAnalyzerUtilities
{
    public static double GetFieldWeight(this IOutputFieldDefinition field)
    {
        // Use weight from @cost directive.
        var costDirective = field.Directives.FirstOrDefaultValue<CostDirective>();

        if (costDirective is not null)
        {
            return costDirective.Weight;
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Fields returning scalar and enum types, arguments of scalar and enum types,
        // as well as input fields of scalar and enum types all default to "0.0"."
        return field.Type.NamedType().IsCompositeType() || field.Type.IsListType() ? 1.0 : 0.0;
    }

    public static double GetFieldWeight(this IInputValueDefinition field)
    {
        // Use weight from @cost directive.
        var costDirective = field.Directives.FirstOrDefaultValue<CostDirective>();

        if (costDirective is not null)
        {
            return costDirective.Weight;
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Fields returning scalar and enum types, arguments of scalar and enum types,
        // as well as input fields of scalar and enum types all default to "0.0"."
        return field.Type.NamedType().IsInputObjectType() ? 1.0 : 0.0;
    }

    public static double GetTypeWeight(this IOutputFieldDefinition field)
    {
        var namedType = field.Type.NamedType();
        var costDirective = namedType.Directives.FirstOrDefaultValue<CostDirective>();

        if (costDirective is not null)
        {
            return costDirective.Weight;
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Weights for all composite input and output types default to "1.0""
        return namedType.IsCompositeType() ? 1.0 : 0.0;
    }

    public static double GetTypeWeight(this IType type)
    {
        var namedType = type.NamedType();
        var costDirective = namedType.Directives.FirstOrDefaultValue<CostDirective>();

        if (costDirective is not null)
        {
            return costDirective.Weight;
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Weights for all composite input and output types default to "1.0""
        return namedType.IsCompositeType() ? 1.0 : 0.0;
    }

    public static double GetListSize(
        this IOutputFieldDefinition field,
        IReadOnlyList<ArgumentNode> arguments,
        ListSizeDirective? listSizeDirective)
    {
        const int defaultListSize = 1;

        if (listSizeDirective is null)
        {
            return defaultListSize;
        }

        if (listSizeDirective.SlicingArguments.Length > 0)
        {
            var index = 0;
            Span<int> slicingValues = stackalloc int[listSizeDirective.SlicingArguments.Length];
            foreach (var slicingArgumentName in listSizeDirective.SlicingArguments)
            {
                var slicingArgument = arguments.SingleOrDefault(a => a.Name.Value == slicingArgumentName);

                if (slicingArgument is not null)
                {
                    switch (slicingArgument.Value)
                    {
                        case IntValueNode intValueNode:
                            slicingValues[index++] = intValueNode.ToInt32();
                            continue;

                        // if one of the slicing arguments is variable we will assume the
                        // maximum allowed page size.
                        case VariableNode when listSizeDirective.AssumedSize.HasValue:
                            return listSizeDirective.AssumedSize.Value;
                    }
                }

                if (field.Arguments.TryGetField(slicingArgumentName, out var argument)
                    && argument.DefaultValue is IntValueNode defaultValueNode)
                {
                    slicingValues[index++] = defaultValueNode.ToInt32();
                }
            }

            if (index == 0 && listSizeDirective.SlicingArgumentDefaultValue.HasValue)
            {
                // if no slicing arguments were found we assume the
                // paging default size if one is set.
                return listSizeDirective.SlicingArgumentDefaultValue.Value;
            }

            if (index == 1)
            {
                return slicingValues[0];
            }

            if (index > 1)
            {
                var max = 0;

                for (var i = 0; i < index; i++)
                {
                    var value = slicingValues[i];
                    if (value > max)
                    {
                        max = value;
                    }
                }

                return max;
            }
        }

        return listSizeDirective.AssumedSize ?? defaultListSize;
    }

    public static void ValidateRequireOneSlicingArgument(
        this ListSizeDirective? listSizeDirective,
        FieldNode node,
        IList<ISyntaxNode> path)
    {
        // The `requireOneSlicingArgument` argument can be used to inform the static analysis
        // that it should expect that exactly one of the defined slicing arguments is present in
        // a query. If that is not the case (i.e., if none or multiple slicing arguments are
        // present), the static analysis may throw an error.
        if (listSizeDirective?.RequireOneSlicingArgument ?? false)
        {
            var argumentCount = 0;
            var variableCount = 0;

            foreach (var argumentNode in node.Arguments)
            {
                if (listSizeDirective.SlicingArguments.Contains(argumentNode.Name.Value))
                {
                    if (argumentNode.Value.Kind == SyntaxKind.NullValue)
                    {
                        continue;
                    }

                    argumentCount++;

                    if (argumentNode.Value.Kind == SyntaxKind.Variable)
                    {
                        variableCount++;
                    }
                }
            }

            if (argumentCount > 0
                && argumentCount == variableCount
                && argumentCount <= listSizeDirective.SlicingArguments.Length)
            {
                return;
            }

            if (argumentCount != 1)
            {
                throw new GraphQLException(ErrorHelper.ExactlyOneSlicingArgMustBeDefined(node, path));
            }
        }
    }
}
