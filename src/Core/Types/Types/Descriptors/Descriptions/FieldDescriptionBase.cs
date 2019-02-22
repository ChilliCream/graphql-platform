using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public abstract class FieldDescriptionBase
        : DescriptionBase
        , IHasDirectiveDescriptions
    {
        /// <summary>
        /// Gets the field type.
        /// </summary>
        public ITypeReference Type { get; set; }

        /// <summary>
        /// Defines if this field is ignored and will
        /// not be included into the schema.
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// Gets the list of directives that are annotated to this field.
        /// </summary>
        public IList<DirectiveDescription> Directives { get; } =
            new List<DirectiveDescription>();

        protected override void OnValidate(ICollection<IError> errors)
        {
            if (!Ignore)
            {
                base.OnValidate(errors);

                if (Type == null)
                {
                    // TODO : resources
                    errors.Add(ErrorBuilder.New()
                        .SetMessage("A field / argument type mustn't be null.")
                        .Build());
                }
            }
        }
    }
}
