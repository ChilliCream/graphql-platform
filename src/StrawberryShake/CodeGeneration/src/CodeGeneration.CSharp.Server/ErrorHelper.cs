using System;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class ErrorHelper
{
    public static GeneratorResponse ExceptionToError(GraphQLException exception)
        => new(errors: new[]
        {
            new GeneratorError(
                ErrorCodes.ParameterValidation,
                ErrorTitles.Generator,
                exception.Message)
        });

    public static GeneratorResponse ExceptionToError(Exception exception)
        => new(errors: new[]
        {
            new GeneratorError(
                ErrorCodes.ParameterValidation,
                ErrorTitles.Generator,
                exception.Message)
        });
}
