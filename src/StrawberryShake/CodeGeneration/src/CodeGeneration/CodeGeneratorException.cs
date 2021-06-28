using System.Collections.Generic;
using System.Runtime.Serialization;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class CodeGeneratorException : GraphQLException
    {
        public CodeGeneratorException(string message)
            : base(message)
        {
        }

        public CodeGeneratorException(IError error)
            : base(error)
        {
        }

        public CodeGeneratorException(params IError[] errors)
            : base(errors)
        {
        }

        public CodeGeneratorException(IEnumerable<IError> errors)
            : base(errors)
        {
        }

        protected CodeGeneratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
