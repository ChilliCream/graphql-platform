using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public abstract class TypeDescriptionBase
    {
        protected TypeDescriptionBase() { }

        /// <summary>
        /// Gets or sets the name the type shall have.
        /// </summary>
        public NameString Name { get; set; }

        // <summary>
        /// Gets or sets the description the type shall have.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the list of directives the type shall be annotated with.
        /// </summary>
        public IList<DirectiveDescription> Directives { get; } =
            new List<DirectiveDescription>();

        /// <summary>
        /// Get access to context data that are copied to the type
        /// and can be used for customizations.
        /// </summary>
        public IDictionary<string, object> ContextData { get; } =
            new Dictionary<string, object>();

        public abstract IDescriptionValidationResult Validate();
    }

    public abstract class TypeDescriptionBase<T>
        : TypeDescriptionBase
        , IHasSyntaxNode
        where T : class, ISyntaxNode
    {
        public T SyntaxNode { get; set; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
    }
}
