using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Utilities
{
    public partial class ExtractFieldQuerySyntaxRewriter
    {
        public class Context
        {
            public Context(
                NameString schema,
                INamedOutputType typeContext,
                DocumentNode document,
                OperationDefinitionNode operation)
            {
                Schema = schema;
                Variables = new Dictionary<string, VariableDefinitionNode>();
                Document = document;
                Operation = operation;
                TypeContext = typeContext;
                Fragments = new Dictionary<string, FragmentDefinitionNode>();
                FragmentPath = ImmutableHashSet<string>.Empty;
            }

            public NameString Schema { get; }

            public DocumentNode Document { get; }

            public OperationDefinitionNode Operation { get; }

            public IDictionary<string, VariableDefinitionNode> Variables { get; }

            public INamedOutputType TypeContext { get; set; }

            public DirectiveType Directive { get; set; }

            public IOutputField OutputField { get; set; }

            public IInputField InputField { get; set; }

            public IInputType InputType { get; set; }

            public ImmutableHashSet<string> FragmentPath { get; set; }

            public IDictionary<string, FragmentDefinitionNode> Fragments
            { get; }

            public Context Clone()
            {
                return (Context)MemberwiseClone();
            }
        }
    }
}
