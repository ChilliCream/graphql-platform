using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class DirectiveTypeDefinition
        : DefinitionBase<DirectiveDefinitionNode>
        , IHasRuntimeType
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
        public Type RuntimeType
        {
            get => _clrType;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _clrType = value;
            }
        }

        /// <summary>
        /// Gets or the associated middleware components.
        /// </summary>
        public IList<DirectiveMiddleware> MiddlewareComponents { get; } =
            new List<DirectiveMiddleware>();

        /// <summary>
        /// Defines the location on which a directive can be annotated.
        /// </summary>
        public ISet<DirectiveLocation> Locations { get; } =
            new HashSet<DirectiveLocation>();

        /// <summary>
        /// Gets the directive arguments.
        /// </summary>
        public IBindableList<DirectiveArgumentDefinition> Arguments { get; } =
            new BindableList<DirectiveArgumentDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();

            configs.AddRange(Configurations);

            foreach (DirectiveArgumentDefinition field in Arguments)
            {
                configs.AddRange(field.Configurations);
            }

            return configs;
        }
    }
}
