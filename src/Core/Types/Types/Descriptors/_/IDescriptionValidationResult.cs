using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptionValidationResult
    {
        bool IsValid { get; }

        bool HasErrors { get; }

        ICollection<IError> Errors { get; }
    }
}
