using System;
using System.Collections.Generic;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Generators
{
    [Serializable]
    public sealed class GeneratorException
        : Exception
    {
        public GeneratorException(string message)
            : this(HCErrorBuilder.New().SetMessage(message).Build())
        {
        }

        public GeneratorException(HotChocolate.IError error)
            : base(error?.Message)
        {
            Errors = error == null
                ? Array.Empty<HotChocolate.IError>()
                : new[] { error };
        }

        public GeneratorException(params HotChocolate.IError[] errors)
        {
            Errors = errors ?? Array.Empty<HotChocolate.IError>();
        }

        public GeneratorException(IEnumerable<HotChocolate.IError> errors)
        {
            Errors = new List<HotChocolate.IError>(
               errors ?? Array.Empty<HotChocolate.IError>())
                   .AsReadOnly();
        }

        public IReadOnlyList<HotChocolate.IError> Errors { get; }
    }
}
