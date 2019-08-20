using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

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

        public IReadOnlyCollection<ISchemaError> Errors { get; }

        private static string CreateErrorMessage(
            IReadOnlyCollection<ISchemaError> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                // TODO : resources
                return "Unexpected schema exception occured.";
            }

            if (errors.Count == 1)
            {
                return CreateErrorMessage(errors.First());
            }

            var message = new StringBuilder();

            // TODO : resources
            message.AppendLine("Multiple schema errors occured:");
            foreach (ISchemaError error in errors)
            {
                message.AppendLine(CreateErrorMessage(error));
            }

            return message.ToString();
        }

        private static string CreateErrorMessage(ISchemaError error)
        {
            if (error.TypeSystemObject == null
                || error.TypeSystemObject.Name.IsEmpty)
            {
                return error.Message;
            }
            else
            {
                return $"{error.Message} - Type: {error.TypeSystemObject.Name}";
            }
        }
    }
}
