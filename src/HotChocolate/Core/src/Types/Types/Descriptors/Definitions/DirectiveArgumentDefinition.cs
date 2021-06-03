using System.Reflection;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// This definition represents a directive argument.
    /// </summary>
    public class DirectiveArgumentDefinition : ArgumentDefinition
    {
        /// <summary>
        /// The property to which this argument binds to.
        /// </summary>
        public PropertyInfo? Property { get; set; }
    }
}
