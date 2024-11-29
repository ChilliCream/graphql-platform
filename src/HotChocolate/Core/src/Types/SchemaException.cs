using System.Diagnostics;
using System.Text;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate;

public sealed class SchemaException : Exception
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
        if (errors is null || errors.Count == 0)
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

            message.AppendLine();
        }

        return message.ToString();
    }
}
