using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using static StrawberryShake.CodeGeneration.ErrorHelper;
using static StrawberryShake.CodeGeneration.CSharp.ErrorCodes;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class ErrorHelper
{
    public static IReadOnlyList<GeneratorError> ConvertErrors(IReadOnlyList<IError> errors)
    {
        var generatorErrors = new GeneratorError[errors.Count];

        for (var i = 0; i < errors.Count; i++)
        {
            generatorErrors[i] = ConvertError(errors[i]);
        }

        return generatorErrors;
    }

    private static GeneratorError ConvertError(IError error)
    {
        var title =
            error.Extensions is not null &&
            error.Extensions.TryGetValue(TitleExtensionKey, out var value) &&
            value is string s ? s : nameof(Unexpected);

        var code = error.Code ?? Unexpected;

        if (error is { Locations: { Count: > 0 } locations, Extensions: { } } &&
            error.Extensions.TryGetValue(FileExtensionKey, out value) &&
            value is string filePath)
        {
            return new GeneratorError(
                code,
                title,
                error.Message,
                filePath,
                ConvertLocation(locations));
        }

        return new GeneratorError(
            code,
            title,
            error.Message);
    }

    private static Location ConvertLocation(IEnumerable<HotChocolate.Location> locations)
    {
        var loc = locations.First();
        return new Location(loc.Line, loc.Column);
    }

    public static GeneratorResponse ExceptionToError(GraphQLException exception)
        => new(errors: ConvertErrors(exception.Errors));

    public static GeneratorResponse ExceptionToError(Exception exception)
        => new(errors: new[]
        {
            new GeneratorError(
                ParameterValidation,
                ErrorTitles.Generator,
                exception.Message)
        });
}
