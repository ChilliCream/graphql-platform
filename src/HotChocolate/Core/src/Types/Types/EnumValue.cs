using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public interface IEnumValue<T> : IEnumValue
    {
        new T Value { get; }
    }

    public interface IEnumValue
        : IHasDirectives
        , IHasContextData
    {
        EnumValueDefinitionNode? SyntaxNode { get; }

        /// <summary>
        /// The GraphQL name of this enum value.
        /// </summary>
        NameString Name { get; }

        string? Description { get; }

        bool IsDeprecated { get; }

        string? DeprecationReason { get; }

        /// <summary>
        /// Gets the runtime value.
        /// </summary>
        object Value { get; }
    }

    public sealed class EnumValue<T>
        : EnumValue
        , IEnumValue<T>
    {
        public EnumValue(
            ITypeCompletionContext completionContext,
            EnumValueDefinition enumValueDefinition)
            : base(completionContext, enumValueDefinition)
        {
            Value = (T)enumValueDefinition.Value!;
        }

        public new T Value { get; }
    }

    public class EnumValue : IEnumValue
    {
        private readonly DirectiveCollection _directives;

        public EnumValue(
            ITypeCompletionContext completionContext,
            EnumValueDefinition enumValueDefinition)
        {
            if (enumValueDefinition is null)
            {
                throw new ArgumentNullException(nameof(enumValueDefinition));
            }

            if (enumValueDefinition.Value is null)
            {
                throw new ArgumentException(
                    TypeResources.EnumValue_ValueIsNull,
                    nameof(enumValueDefinition));
            }

            SyntaxNode = enumValueDefinition.SyntaxNode;
            Name = enumValueDefinition.Name.HasValue
                ? enumValueDefinition.Name
                : (NameString)enumValueDefinition.Value.ToString();
            Description = enumValueDefinition.Description;
            DeprecationReason = enumValueDefinition.DeprecationReason;
            IsDeprecated = !string.IsNullOrEmpty(
                enumValueDefinition.DeprecationReason);
            Value = enumValueDefinition.Value;
            ContextData = enumValueDefinition.ContextData;

            _directives = new DirectiveCollection(this, enumValueDefinition!.Directives);
            _directives.CompleteCollection(completionContext);
        }

        public EnumValueDefinitionNode? SyntaxNode { get; }

        public NameString Name { get; }

        public string? Description { get; }

        public bool IsDeprecated { get; }

        public string? DeprecationReason { get; }

        public object Value { get; }

        public IDirectiveCollection Directives => _directives;

        public IReadOnlyDictionary<string, object?> ContextData { get; }
    }
}
