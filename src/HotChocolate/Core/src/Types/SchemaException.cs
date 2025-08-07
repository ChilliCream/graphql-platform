#nullable enable

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate;

public sealed partial class SchemaException : Exception
{
    public SchemaException(params ISchemaError[] errors)
        : base(CreateErrorMessage(errors))
    {
        Errors = errors;
        Debug.WriteLine(Message);
    }

    public SchemaException(IReadOnlyList<ISchemaError> errors)
        : base(CreateErrorMessage(errors))
    {
        Errors = errors.ToArray();
        Debug.WriteLine(Message);
    }

    public IReadOnlyList<ISchemaError> Errors { get; }

    private static string CreateErrorMessage(IReadOnlyList<ISchemaError> errors)
    {
        if (errors.Count == 0)
        {
            return SchemaException_UnexpectedError;
        }

        var message = new StringBuilder();

        message.AppendLine(SchemaException_ErrorSummaryText);
        message.AppendLine();

        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];

            message.Append($"{i + 1}. {error.Message}");

            if (error.TypeSystemObject is not null)
            {
                message.Append($" ({error.TypeSystemObject.GetType().GetTypeName()})");
            }

            if (error.Exception is { StackTrace: not null })
            {
                message.AppendLine();
                message.AppendLine();
                message.Append(StackTraceHelper.Normalize(error.Exception.StackTrace));
                message.AppendLine();
            }

            message.AppendLine();
        }

        return message.ToString();
    }

    private static partial class StackTraceHelper
    {
        [GeneratedRegex(@" in ([^:]+):line (\d+)", RegexOptions.Compiled)]
        private static partial Regex StackTracePathRegex();

        public static string? Normalize(string stackTrace)
        {
            return StackTracePathRegex().Replace(stackTrace, match =>
            {
                var fullPath = match.Groups[1].Value;
                var lineNumber = match.Groups[2].Value;

                var fileName = System.IO.Path.GetFileName(fullPath);
                return $" in {fileName}:line {lineNumber}";
            });
        }
    }
}
