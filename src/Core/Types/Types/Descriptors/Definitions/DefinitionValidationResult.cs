using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public sealed class DefinitionValidationResult
        : IDefinitionValidationResult
    {
        public DefinitionValidationResult(IReadOnlyList<IError> errors)
        {
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        public bool IsValid => Errors.Count == 0;

        public bool HasErrors => Errors.Count > 0;

        public IReadOnlyList<IError> Errors { get; }
    }
}
