using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Execution.Utilities
{
    public static class ArgumentCoercionHelper
    {
        public static bool TryCoerceArguments(
            this IPreparedArgumentMap arguments,
            IVariableValueCollection variables,
            Action<IError> reportError,
            [NotNullWhen(true)] out IReadOnlyDictionary<NameString, PreparedArgument>? coercedArgs)
        {
            // if we have errors on the compiled execution plan we will report the errors and
            // signal that this resolver task has errors and shall end.
            if (arguments.HasErrors)
            {
                foreach (PreparedArgument argument in arguments.Values)
                {
                    if (argument.HasError)
                    {
                        reportError(argument.Error!);
                    }
                }

                coercedArgs = null;
                return false;
            }

            if (arguments.IsFinal)
            {
                coercedArgs = arguments;
                return true;
            }

            // if there are arguments that have variables and need variable replacement we will
            // rewrite the arguments that need variable replacement.
            var args = new Dictionary<NameString, PreparedArgument>();

            foreach (PreparedArgument argument in arguments.Values)
            {
                if (argument.IsFinal)
                {
                    args.Add(argument.Argument.Name, argument);
                }
                else
                {
                    IValueNode literal = VariableRewriter.Rewrite(
                        argument.ValueLiteral!, variables);

                    args.Add(argument.Argument.Name, new PreparedArgument(
                        argument.Argument,
                        literal.TryGetValueKind(out ValueKind kind)
                            ? kind
                            : ValueKind.Unknown,
                        argument.IsFinal,
                        argument.IsImplicit,
                        null,
                        literal));
                }
            }

            coercedArgs = args;
            return true;
        }
    }
}
