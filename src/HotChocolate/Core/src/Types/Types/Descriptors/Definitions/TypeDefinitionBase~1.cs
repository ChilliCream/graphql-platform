using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable  enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class TypeDefinitionBase<T>
        : DefinitionBase<T>
        , ITypeDefinition
        where T : class, ISyntaxNode
    {
        private List<DirectiveDefinition>? _directives;

        protected TypeDefinitionBase() { }

        private Type? _clrType;

        /// <summary>
        /// Gets or sets the .net type representation of this type.
        /// </summary>
        public virtual Type RuntimeType
        {
            get => _clrType;
            set
            {
                _clrType = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Gets the list of directives that are annotated to this type.
        /// </summary>
        public IList<DirectiveDefinition> Directives =>
            _directives ??= new List<DirectiveDefinition>();

        /// <summary>
        /// Gets the list of directives that are annotated to this field.
        /// </summary>
        public IReadOnlyList<DirectiveDefinition> GetDirectives()
        {
            if (_directives is null)
            {
                return Array.Empty<DirectiveDefinition>();
            }

            return _directives;
        }
    }
}
