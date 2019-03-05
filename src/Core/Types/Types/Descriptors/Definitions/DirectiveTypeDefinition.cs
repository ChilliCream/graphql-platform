using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class DirectiveTypeDefinition
        : DefinitionBase<DirectiveDefinitionNode>
        , IHasClrType
    {
        private Type _clrType = typeof(object);

        /// <summary>
        /// Defines if this directive can be specified multiple
        /// times on the same object.
        /// </summary>
        public bool IsRepeatable { get; set; }

        /// <summary>
        /// Gets or sets the .net type representation of this directive.
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
        /// Gets or sets the associated field middleware.
        /// </summary>
        public IDirectiveMiddleware Middleware { get; set; }

        /// <summary>
        /// Defines the location on which a directive can be annotated.
        /// </summary>
        public ISet<DirectiveLocation> Locations { get; } =
            new HashSet<DirectiveLocation>();

        /// <summary>
        /// Gets the directive arguments.
        /// </summary>
        public IBindableList<DirectiveArgumentDefinition> Arguments
        { get; } = new BindableList<DirectiveArgumentDefinition>();

        protected override void OnValidate(ICollection<IError> errors)
        {
            base.OnValidate(errors);

            if (Locations.Count == 0)
            {
                // TODO : resources
                errors.Add(ErrorBuilder.New()
                    .SetMessage(
                        "A directive must at least specify one location " +
                        "on which it is valid.")
                    .Build());
            }

            foreach (IError argumentError in Arguments
                .SelectMany(a => a.Validate().Errors))
            {
                errors.Add(argumentError);
            }
        }
    }
}
