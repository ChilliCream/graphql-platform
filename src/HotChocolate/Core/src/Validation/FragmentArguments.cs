using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation;

/// <summary>
/// Resolves the declaration that an argument of a fragment spread refers to.
/// </summary>
internal static class FragmentArguments
{
    /// <summary>
    /// Resolves the type that the argument named <paramref name="argumentName"/> must satisfy.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> if the spread targets an unknown fragment, if the fragment does
    /// not declare a variable with that name, or if the declared type is not in the schema.
    /// </returns>
    public static bool TryGetArgumentType(
        DocumentValidatorContext context,
        FragmentSpreadNode spread,
        string argumentName,
        [NotNullWhen(true)] out IType? argumentType)
    {
        if (TryGetVariableDefinition(context, spread, argumentName, out var variableDefinition))
        {
            return context.Schema.Types.TryGetType(variableDefinition.Type, out argumentType);
        }

        argumentType = null;
        return false;
    }

    /// <summary>
    /// Resolves the variable definition that the argument named <paramref name="argumentName"/>
    /// provides a value for.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> if the spread targets an unknown fragment or if the fragment does
    /// not declare a variable with that name.
    /// </returns>
    public static bool TryGetVariableDefinition(
        DocumentValidatorContext context,
        FragmentSpreadNode spread,
        string argumentName,
        [NotNullWhen(true)] out VariableDefinitionNode? variableDefinition)
    {
        if (context.Fragments.TryGet(spread, out var fragment))
        {
            var variableDefinitions = fragment.VariableDefinitions;

            for (var i = 0; i < variableDefinitions.Count; i++)
            {
                var candidate = variableDefinitions[i];

                if (candidate.Variable.Name.Value.Equals(argumentName, StringComparison.Ordinal))
                {
                    variableDefinition = candidate;
                    return true;
                }
            }
        }

        variableDefinition = null;
        return false;
    }
}
