using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface IHasDirectiveDefinition
    {
        /// <summary>
        /// Gets the list of directives that are annotated to
        /// the implementing object.
        /// </summary>
        IList<DirectiveDefinition> Directives { get; }

        /// <summary>
        /// Gets the list of directives that are annotated to
        /// the implementing object.
        /// </summary>
        IReadOnlyList<DirectiveDefinition> GetDirectives();
    }
}
