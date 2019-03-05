using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class DefinitionBase
    {
        protected DefinitionBase() { }

        /// <summary>
        /// Gets or sets the name the type shall have.
        /// </summary>
        public NameString Name { get; set; }

        // <summary>
        /// Gets or sets the description the type shall have.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Get access to context data that are copied to the type
        /// and can be used for customizations.
        /// </summary>
        public IDictionary<string, object> ContextData { get; } =
            new Dictionary<string, object>();

        /// <summary>
        /// Validates the description object for consitency and
        /// returns the validation results.
        /// </summary>
        public IDescriptionValidationResult Validate()
        {
            var errors = new List<IError>();
            OnValidate(errors);
            return new DescriptionValidationResult(errors);
        }

        protected virtual void OnValidate(ICollection<IError> errors)
        {
            if (Name.IsEmpty)
            {
                // TODO : resources
                errors.Add(ErrorBuilder.New()
                    .SetMessage(
                        "A type- / field-description object name" +
                        "mustn't be null.")
                    .Build());
            }
        }
    }
}
