using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class Directive
        : ITypeSystemNode
    {
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyCollection<DirectiveLocation> Locations { get; }
        public IReadOnlyCollection<object> Arguments { get; }
        public DirectiveDefinitionNode SyntaxNode { get; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            yield break;
        }
    }

    public enum DirectiveLocation
    {
        Query,
        Mutation,
        Subscription,
        Field,
        FragmentDefinition,
        FragmentSpread,
        InlineFragment,
        Schema,
        Scalar,
        Object,
        FieldDefinition,
        ArgumentDefinition,
        Interface,
        Union,
        Enum,
        EnumValue,
        InputObject,
        InputFieldDefinition
    }
}
