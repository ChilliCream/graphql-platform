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
        public SchemaException(params SchemaError[] errors)
            : base(CreateErrorMessage(errors))
        {
            Errors = errors;
            Debug.WriteLine(Message);
        }

        public SchemaException(IEnumerable<SchemaError> errors)
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

        public IReadOnlyCollection<SchemaError> Errors { get; }

        private static string CreateErrorMessage(
            IReadOnlyCollection<SchemaError> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return "Unexpected schema exception occured.";
            }

            if (errors.Count == 1)
            {
                return CreateErrorMessage(errors.First());
            }

            var message = new StringBuilder();

            message.AppendLine("Multiple schema errors occured:");
            foreach (SchemaError error in errors)
            {
                message.AppendLine(CreateErrorMessage(error));
            }

            return message.ToString();
        }

        private static string CreateErrorMessage(SchemaError error)
        {
            if (error.Type == null)
            {
                return error.Message;
            }
            else
            {
                return $"{error.Message} - Type: {error.Type.Name}";
            }
        }
    }
}
