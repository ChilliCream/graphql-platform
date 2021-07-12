using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Defines the properties of a GraphQL directive.
    /// </summary>
    public class DirectiveTypeDefinition
        : DefinitionBase<DirectiveDefinitionNode>
        , IHasRuntimeType
    {
        private Type _clrType = typeof(object);
        private List<DirectiveMiddleware>? _middlewareComponents;
        private HashSet<DirectiveLocation>? _locations;
        private BindableList<DirectiveArgumentDefinition>? _arguments;

        /// <summary>
        /// Initializes a new instance of <see cref="DirectiveTypeDefinition"/>.
        /// </summary>
        public DirectiveTypeDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DirectiveTypeDefinition"/>.
        /// </summary>
        public DirectiveTypeDefinition(
            NameString name,
            string? description = null,
            Type? runtimeType = null,
            bool isRepeatable = false)
        {
            Name = name;
            Description = description;
            RuntimeType = runtimeType ?? typeof(object);
            IsRepeatable = isRepeatable;
        }

        /// <summary>
        /// Defines if this directive can be specified multiple
        /// times on the same object.
        /// </summary>
        public bool IsRepeatable { get; set; }

        /// <summary>
        /// Defines if this directive is visible when directive introspection is enabled.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the .net type representation of this directive.
        /// </summary>
        public Type RuntimeType
        {
            get => _clrType;
            set
            {
                _clrType = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Gets or the associated middleware components.
        /// </summary>
        public IList<DirectiveMiddleware> MiddlewareComponents =>
            _middlewareComponents ??= new List<DirectiveMiddleware>();

        /// <summary>
        /// Defines the location on which a directive can be annotated.
        /// </summary>
        public ISet<DirectiveLocation> Locations => _locations ??= new HashSet<DirectiveLocation>();

        /// <summary>
        /// Gets the directive arguments.
        /// </summary>
        public IBindableList<DirectiveArgumentDefinition> Arguments =>
            _arguments ??= new BindableList<DirectiveArgumentDefinition>();

        internal override IEnumerable<ILazyTypeConfiguration> GetConfigurations()
        {
            var configs = new List<ILazyTypeConfiguration>();

            configs.AddRange(Configurations);

            foreach (DirectiveArgumentDefinition field in GetArguments())
            {
                configs.AddRange(field.Configurations);
            }

            return configs;
        }

        /// <summary>
        /// Gets or the associated middleware components.
        /// </summary>
        internal IReadOnlyList<DirectiveMiddleware> GetMiddlewareComponents()
        {
            if (_middlewareComponents is null)
            {
                return Array.Empty<DirectiveMiddleware>();
            }

            return _middlewareComponents;
        }

        /// <summary>
        /// Defines the location on which a directive can be annotated.
        /// </summary>
        internal IReadOnlyCollection<DirectiveLocation> GetLocations()
        {
            if (_locations is null)
            {
                return Array.Empty<DirectiveLocation>();
            }

            return _locations;
        }

        /// <summary>
        /// Gets the directive arguments.
        /// </summary>
        internal IReadOnlyList<DirectiveArgumentDefinition> GetArguments()
        {
            if (_arguments is null)
            {
                return Array.Empty<DirectiveArgumentDefinition>();
            }

            return _arguments;
        }
    }
}
