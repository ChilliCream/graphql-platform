#nullable enable

namespace HotChocolate.Types
{
    internal interface IInheritableAttribute
    {
        /// <summary>
        /// Defines if this attribute is inherited. The default is <c>false</c>.
        /// </summary>
        public bool Inherited { get; set; }
    }
}
