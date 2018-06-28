using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class Directive
        : ITypeSystemNode
        , INeedsInitialization
    {
        internal Directive(DirectiveConfig config)
        {
            Initialize(config);
        }

        public DirectiveDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyCollection<DirectiveLocation> Locations { get; private set; }

        public IReadOnlyDictionary<string, InputField> Arguments { get; private set; }

        #region ITypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            return Arguments.Values;
        }

        #endregion

        #region  Initialization

        private void Initialize(DirectiveConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A directive name must not be null or empty.",
                    nameof(config));
            }

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
            Locations = config.Locations.ToImmutableList(); ;
            Arguments = config.Arguments.ToImmutableDictionary(t => t.Name);
        }

        void INeedsInitialization.RegisterDependencies(ISchemaContext schemaContext, Action<SchemaError> reportError)
        {
            foreach (InputField field in Arguments.Values)
            {
                field.RegisterDependencies(
                    schemaContext.Types, reportError, null);
            }
        }

        void INeedsInitialization.CompleteType(ISchemaContext schemaContext, Action<SchemaError> reportError)
        {
            foreach (InputField field in Arguments.Values)
            {
                field.CompleteInputField(
                    schemaContext.Types, reportError, null);
            }
        }

        #endregion
    }

    internal class DirectiveConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<DirectiveLocation> Locations { get; set; }
        public IEnumerable<InputField> Arguments { get; set; }
        public DirectiveDefinitionNode SyntaxNode { get; set; }
    }
}
