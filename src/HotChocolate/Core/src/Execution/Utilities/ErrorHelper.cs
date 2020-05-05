using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal static class ErrorHelper
    {
        public static IError ArgumentNonNullError(
            ArgumentNode argument,
            string responseName,
            ArgumentNonNullValidator.ValidationResult validationResult)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    "Detected a non-null violation in argument `{0}`.",
                    argument.Name.Value)
                .AddLocation(argument)
                .SetExtension("responseName", responseName)
                .SetExtension("errorPath", validationResult.Path)
                .Build();
        }

        public static IError ArgumentValueIsInvalid(
            ArgumentNode argument,
            string responseName,
            ScalarSerializationException exception)
        {
            return ErrorBuilder.New()
                .SetMessage(exception.Message)
                .AddLocation(argument)
                .SetExtension("responseName", responseName)
                .Build();
        }

        public static IError ArgumentDefaultValueIsInvalid(
            string responseName,
            ScalarSerializationException exception)
        {
            return ErrorBuilder.New()
                .SetMessage(exception.Message)
                .SetExtension("responseName", responseName)
                .Build();
        }
    }
}
