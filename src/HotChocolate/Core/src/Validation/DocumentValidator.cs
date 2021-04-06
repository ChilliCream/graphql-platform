using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// The default document validator implementation.
    /// </summary>
    public sealed class DocumentValidator : IDocumentValidator
    {
        private readonly DocumentValidatorContextPool _contextPool;
        private readonly IDocumentValidatorRule[] _rules;

        /// <summary>
        /// Creates a new instance of <see cref="DocumentValidator"/>.
        /// </summary>
        /// <param name="contextPool">
        /// The document validator context pool.
        /// </param>
        /// <param name="rules">
        /// The validation rules.
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        public DocumentValidator(
            DocumentValidatorContextPool contextPool,
            IEnumerable<IDocumentValidatorRule> rules)
        {
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            _contextPool = contextPool ?? throw new ArgumentNullException(nameof(contextPool));
            _rules = rules.ToArray();
        }

        /// <inheritdoc />
        public DocumentValidatorResult Validate(
            ISchema schema,
            DocumentNode document) =>
            Validate(schema, document, null);

        /// <inheritdoc />
        public DocumentValidatorResult Validate(
            ISchema schema,
            DocumentNode document,
            IEnumerable<KeyValuePair<string, object?>>? contextData)
        {
            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            DocumentValidatorContext context = _contextPool.Get();

            try
            {
                PrepareContext(schema, document, context, contextData);

                for (var i = 0; i < _rules.Length; i++)
                {
                    _rules[i].Validate(context, document);
                }

                return context.Errors.Count > 0
                    ? new DocumentValidatorResult(context.Errors)
                    : DocumentValidatorResult.Ok;
            }
            finally
            {
                _contextPool.Return(context);
            }
        }

        private void PrepareContext(
            ISchema schema,
            DocumentNode document,
            DocumentValidatorContext context,
            IEnumerable<KeyValuePair<string, object?>>? contextData)
        {
            context.Schema = schema;

            for (var i = 0; i < document.Definitions.Count; i++)
            {
                IDefinitionNode definitionNode = document.Definitions[i];
                if (definitionNode.Kind == SyntaxKind.FragmentDefinition)
                {
                    var fragmentDefinition = (FragmentDefinitionNode)definitionNode;
                    context.Fragments[fragmentDefinition.Name.Value] = fragmentDefinition;
                }
            }

            if (contextData is not null)
            {
                foreach (KeyValuePair<string, object?> entry in contextData)
                {
                    context.ContextData[entry.Key] = entry.Value;
                }
            }
        }
    }
}
