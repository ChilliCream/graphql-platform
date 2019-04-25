using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class SchemaTypeDefinition
        : DefinitionBase<SchemaDefinitionNode>
        , IHasDirectiveDefinition
    {
        /// <summary>
        /// Gets the list of directives that are annotated to this schema.
        /// </summary>
        public IList<DirectiveDefinition> Directives { get; } =
            new List<DirectiveDefinition>();
    }
}
