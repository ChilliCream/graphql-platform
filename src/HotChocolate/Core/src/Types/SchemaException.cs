using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using HotChocolate.Utilities;

namespace HotChocolate
{
    [Serializable]
    public class SchemaException
        : Exception
    {
        public SchemaException(params ISchemaError[] errors)
            : base(CreateErrorMessage(errors))
        {
            Errors = errors;
            Debug.WriteLine(Message);
        }

        public SchemaException(IEnumerable<ISchemaError> errors)
            : base(CreateErrorMessage(errors.ToArray()))
        {
            Errors = errors.ToArray();
            Debug.WriteLine(Message);
        }

        protected SchemaException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        public IReadOnlyList<ISchemaError> Errors { get; }

        // TODO : resources
        private static string CreateErrorMessage(IReadOnlyList<ISchemaError> errors)
        {
            if (errors is null || errors.Count == 0)
            {
                return "Unexpected schema exception occurred.";
            }

            var message = new StringBuilder();

            message.AppendLine("For more details look at the `Errors` property.");
            message.AppendLine();

            for (int i = 0; i < errors.Count; i++)
            {
                ISchemaError error = errors[i];

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
}
