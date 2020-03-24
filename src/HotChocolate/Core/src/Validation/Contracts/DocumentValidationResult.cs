using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Validation
{
    public class DocumentValidationResult
    {
        private DocumentValidationResult()
        {
            Errors = Array.Empty<IError>();
            HasErrors = false;
        }

        public DocumentValidationResult(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            Errors = new IError[] { error };
            HasErrors = true;
        }

        public DocumentValidationResult(IEnumerable<IError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Errors = errors.ToArray();
            HasErrors = errors.Any();
        }

        public bool HasErrors { get; }

        public IReadOnlyCollection<IError> Errors { get; }

        public static DocumentValidationResult OK { get; } =
            new DocumentValidationResult();
    }
}
