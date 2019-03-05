﻿using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public abstract class FieldDefinitionBase<T>
        : FieldDefinitionBase
        , IHasSyntaxNode
        where T : class, ISyntaxNode
    {
        /// <summary>
        /// The associated syntax node from the GraphQL schema SDL.
        /// </summary>
        public T SyntaxNode { get; set; }

        /// <summary>
        /// The associated syntax node from the GraphQL schema SDL.
        /// </summary>
        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
    }
}
