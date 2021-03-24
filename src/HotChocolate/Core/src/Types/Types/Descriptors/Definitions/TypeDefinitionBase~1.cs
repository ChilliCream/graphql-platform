using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable  enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// A definition that represents a type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TypeDefinitionBase<T>
        : DefinitionBase<T>
        , ITypeDefinition
        where T : class, ISyntaxNode
    {
        private List<DirectiveDefinition>? _directives;

        protected TypeDefinitionBase() { }

        private Type _runtimeType = typeof(object);

        /// <summary>
        /// Gets or sets the .net type representation of this type.
        /// </summary>
        public virtual Type RuntimeType
        {
            get => _runtimeType;
            set
            {
                _runtimeType = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// If this is a type definition extension this is the type we want to extend.
        /// </summary>
        public Type? ExtendsType { get; set; }

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

        protected void CopyTo(TypeDefinitionBase<T> target)
        {
            base.CopyTo(target);

            target._runtimeType = _runtimeType;
            target.ExtendsType = ExtendsType;

            if (_directives is not null and { Count: > 0 })
            {
                target._directives = new List<DirectiveDefinition>(_directives);
            }
        }

        protected void MergeInto(TypeDefinitionBase<T> target)
        {
            base.MergeInto(target);

            // Note: we will not change ExtendsType or _runtimeType on merge.

            if (_directives is not null and { Count: > 0 })
            {
                target._directives ??= new List<DirectiveDefinition>();
                target._directives.AddRange(Directives);
            }
        }
    }
}
