using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class TypeDescriptionBase<T>
        : DescriptionBase<T>
        , IHasClrType
        , IHasDirectiveDescriptions
        where T : class, ISyntaxNode
    {
        protected TypeDescriptionBase() { }

        private Type _clrType;

        /// <summary>
        /// Gets or sets the .net type representation of this type.
        /// </summary>
        public Type ClrType
        {
            get => _clrType;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _clrType = value;
            }
        }

        /// <summary>
        /// Gets the list of directives that are annotated to this type.
        /// </summary>
        public IList<DirectiveDescription> Directives { get; } =
            new List<DirectiveDescription>();
    }
}
