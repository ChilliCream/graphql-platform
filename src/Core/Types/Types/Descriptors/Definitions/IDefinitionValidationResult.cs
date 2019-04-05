using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface IDefinitionValidationResult
    {
        bool IsValid { get; }

        bool HasErrors { get; }

        IReadOnlyList<IError> Errors { get; }
    }
}
