using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal static class ErrorHelper
    {
        public static IError ArgumentNonNullError(
            string responseName,
            ArgumentNode argument,
            Path path,
            ArgumentNonNullValidator.ValidationResult validationResult)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    "Detected a non-null violation in argument `{0}`.",
                    argument.Name.Value)
                .AddLocation(argument)
                .SetPath(path)
                .SetExtension(_argumentProperty, report.Path.ToCollection())

                .Build();
        }
    }
}
