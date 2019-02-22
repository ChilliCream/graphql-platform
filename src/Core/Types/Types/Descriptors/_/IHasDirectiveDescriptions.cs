using System.Collections.Generic;

namespace HotChocolate.Types.Descriptors
{
    public interface IHasDirectiveDescriptions
    {
        /// <summary>
        /// Gets the list of directives that are annotated to
        /// the implementing object.
        /// </summary>
        IList<DirectiveDescription> Directives { get; }
    }
}
