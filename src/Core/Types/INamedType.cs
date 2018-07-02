using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface INamedType
        : IType
        , INullableType
    {
        string Name { get; }
        string Description { get; }
    }
    public interface IComplexOutputType
        : INamedOutputType
    {
        IReadOnlyDictionary<string, IOutputField> Fields { get; }
    }

    public interface IOutputField
       : IField
    {
        FieldDefinitionNode SyntaxNode { get; }

        bool IsDeprecated { get; }

        string DeprecationReason { get; }

        IOutputType Type { get; }

        IReadOnlyDictionary<string, IInputField> Arguments { get; }

        FieldResolverDelegate Resolver { get; }
    }


    /// <summary>
    /// Represents an input field. Input fields can be arguments of fields
    /// or fields of an input objects.
    /// </summary>
    public interface IInputField
        : IField
    {
        /// <summary>
        /// Gets the associated syntax node.
        /// </summary>
        InputValueDefinitionNode SyntaxNode { get; }

        /// <summary>
        /// Gets the type of this input field.
        /// </summary>
        IInputType Type { get; }

        /// <summary>
        /// Gets the default value literal of this field.
        /// </summary>
        IValueNode DefaultValue { get; }
    }
}
