using System.Collections.Generic;
using HotChocolate.Internal;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public abstract class FieldDefinitionBase
        : DefinitionBase
        , IHasDirectiveDefinition
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
        public IList<DirectiveDefinition> Directives { get; } =
            new List<DirectiveDefinition>();
    }
}
