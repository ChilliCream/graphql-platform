using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Internal;

public static class ArgumentCoercionHelper
{
    public static bool TryCoerceArguments(
        this IArgumentMap arguments,
        IResolverContext resolverContext,
        [NotNullWhen(true)] out IReadOnlyDictionary<string, ArgumentValue>? coercedArgs)
    {
        if (arguments.IsFinalNoErrors)
        {
            coercedArgs = arguments;
            return true;
        }

        // if we have errors on the compiled execution plan we will report the errors and
        // signal that this resolver task has errors and shall end.
        if (arguments.HasErrors)
        {
            foreach (var argument in arguments.Values)
            {
                if (argument.HasError)
                {
                    resolverContext.ReportError(argument.Error!);
                }
            }

            coercedArgs = null;
            return false;
        }

        // if there are arguments that have variables and need variable replacement we will
        // rewrite the arguments that need variable replacement.
        Dictionary<string, ArgumentValue> args = new(StringComparer.Ordinal);

        foreach (var argument in arguments.Values)
        {
            if (argument.IsFullyCoerced)
            {
                args.Add(argument.Name, argument);
            }
            else
            {
                var literal = VariableRewriter.Rewrite(
                    argument.ValueLiteral!,
                    argument.Type,
                    argument.DefaultValue,
                    resolverContext.Variables);

                args.Add(argument.Name, new ArgumentValue(
                    argument,
                    literal.TryGetValueKind(out var kind) ? kind : ValueKind.Unknown,
                    argument.IsFullyCoerced,
                    argument.IsDefaultValue,
                    null,
                    literal));
            }
        }

        coercedArgs = args;
        return true;
    }
}
