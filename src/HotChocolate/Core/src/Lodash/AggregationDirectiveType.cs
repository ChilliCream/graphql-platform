using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.DirectiveLocation;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Lodash
{
    public abstract class AggregationDirectiveType<T>
        : DirectiveType<T>
        , IAggregationDirectiveType
        where T : class
    {
        protected static DirectiveLocation DefaultDirectiveLocation =
            Field | Mutation | Query | Subscription;

        private Func<ISchema> _schemaResolver = null!;
        private ISchema? _schema;

        protected abstract AggregationOperation CreateOperation(T directive);

        protected override void OnAfterCompleteType(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            _schemaResolver = context.GetSchemaResolver();
            base.OnAfterCompleteType(context, definition, contextData);
        }

        public AggregationOperation CreateOperation(DirectiveNode directive)
        {
            T parsedDirective = Directive
                .FromAstNode(GetSchema(), directive, directive)
                .ToObject<T>();

            return CreateOperation(parsedDirective);
        }

        private ISchema GetSchema()
        {
            if (_schema is null)
            {
                _schema = _schemaResolver();
            }

            return _schema;
        }
    }
}
