using System;
using HotChocolate.Language;
using static HotChocolate.Properties.AbstractionResources;

#nullable enable

namespace HotChocolate
{
    /// <summary>
    /// This attributes specifies the GraphQL type a field or argument.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property
        | AttributeTargets.Method
        | AttributeTargets.Parameter)]
    public sealed class GraphQLTypeAttribute : Attribute
    {
        /// <summary>
        /// Specifies the GraphQL types.
        /// </summary>
        public GraphQLTypeAttribute(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Specifies the GraphQL types with GraphQL SLD type syntax.
        /// </summary>
        public GraphQLTypeAttribute(string typeSyntax)
        {
            if (string.IsNullOrEmpty(typeSyntax))
            {
                throw new ArgumentException(
                    GraphQLTypeAttribute_TypeSyntaxCannotBeNullOrEmpty,
                    nameof(typeSyntax));
            }

            TypeSyntax = Utf8GraphQLParser.Syntax.ParseTypeReference(typeSyntax);
        }

        /// <summary>
        /// Gets the GraphQL type if it was specified..
        /// </summary>
        public Type? Type { get; }

        /// <summary>
        /// Gets the GraphQL SDL type syntax if it was specified.
        /// </summary>
        public ITypeNode? TypeSyntax { get;  }
    }
}
