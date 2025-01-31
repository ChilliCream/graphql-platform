using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Errors;

internal static class ErrorHelper
{
    public static CompositionError SourceSchemaParsingFailed()
        => new(ErrorHelper_SourceSchemaParsingFailed);

    public static CompositionError SourceSchemaValidationFailed()
        => new(ErrorHelper_SourceSchemaValidationFailed);

    public static CompositionError PreMergeValidationFailed()
        => new(ErrorHelper_PreMergeValidationFailed);
}
