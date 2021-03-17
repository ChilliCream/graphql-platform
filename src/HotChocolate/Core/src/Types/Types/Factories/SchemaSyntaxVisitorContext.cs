using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Factories
{
    internal class SchemaSyntaxVisitorContext : ISyntaxVisitorContext
    {
        public SchemaSyntaxVisitorContext(
            IBindingLookup bindingLookup,
            IReadOnlySchemaOptions schemaOptions)
        {
            BindingLookup = bindingLookup;
            SchemaOptions = schemaOptions;
        }

        public List<ITypeReference> Types { get; } = new();

        public IReadOnlyCollection<DirectiveNode> Directives { get; set; }

        public string QueryTypeName { get; set; }

        public string MutationTypeName { get; set; }

        public string SubscriptionTypeName { get; set; }

        public string Description { get; set; }

        public IBindingLookup BindingLookup { get; }

        public IReadOnlySchemaOptions SchemaOptions { get; }
    }
}
