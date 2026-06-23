using System.Collections.Frozen;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Brokers;

internal sealed class SubscriptionFieldContext : ISubscriptionFieldContext
{
    private static readonly IReadOnlyDictionary<string, IValueNode> s_emptyArguments =
        new Dictionary<string, IValueNode>().ToFrozenDictionary(StringComparer.Ordinal);

    private readonly Lazy<IReadOnlyDictionary<string, IValueNode>> _arguments;

    public SubscriptionFieldContext(OperationPlanContext context, string fieldName)
    {
        _arguments = new Lazy<IReadOnlyDictionary<string, IValueNode>>(
            () => CreateArguments(context, fieldName));
    }

    public IReadOnlyDictionary<string, IValueNode> Arguments => _arguments.Value;

    private static IReadOnlyDictionary<string, IValueNode> CreateArguments(
        OperationPlanContext context,
        string fieldName)
    {
        var rootField = GetRootField(context.OperationPlan.Operation.Definition, fieldName);

        if (rootField.Arguments.Count == 0)
        {
            return s_emptyArguments;
        }

        var arguments = new Dictionary<string, IValueNode>(
            rootField.Arguments.Count,
            StringComparer.Ordinal);

        foreach (var argument in rootField.Arguments)
        {
            arguments.Add(
                argument.Name.Value,
                argument.Value is VariableNode variable
                    ? context.Variables.GetValue<IValueNode>(variable.Name.Value)
                    : argument.Value);
        }

        return arguments.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static FieldNode GetRootField(OperationDefinitionNode definition, string responseName)
    {
        foreach (var selection in definition.SelectionSet.Selections)
        {
            if (selection is FieldNode field
                && !field.Name.Value.Equals(IntrospectionFieldNames.TypeName, StringComparison.Ordinal)
                && (field.Alias?.Value ?? field.Name.Value).Equals(responseName, StringComparison.Ordinal))
            {
                return field;
            }
        }

        throw new InvalidOperationException(
            $"The subscription root field '{responseName}' was not found.");
    }
}
