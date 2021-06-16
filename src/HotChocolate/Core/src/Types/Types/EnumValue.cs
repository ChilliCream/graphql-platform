using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public sealed class EnumValue : IEnumValue
    {
        private readonly IDirectiveCollection _directives;

        public EnumValue(
            ITypeCompletionContext completionContext,
            EnumValueDefinition enumValueDefinition)
        {
            if (completionContext == null)
            {
                throw new ArgumentNullException(nameof(completionContext));
            }

            if (enumValueDefinition is null)
            {
                throw new ArgumentNullException(nameof(enumValueDefinition));
            }

            if (enumValueDefinition.RuntimeValue is null)
            {
                throw new ArgumentException(
                    TypeResources.EnumValue_ValueIsNull,
                    nameof(enumValueDefinition));
            }

            SyntaxNode = enumValueDefinition.SyntaxNode;
            Name = enumValueDefinition.Name.HasValue
                ? enumValueDefinition.Name
                : (NameString)enumValueDefinition.RuntimeValue.ToString();
            Description = enumValueDefinition.Description;
            DeprecationReason = enumValueDefinition.DeprecationReason;
            IsDeprecated = !string.IsNullOrEmpty(enumValueDefinition.DeprecationReason);
            Value = enumValueDefinition.RuntimeValue;
            ContextData = enumValueDefinition.GetContextData();

            _directives = DirectiveCollection
                .CreateAndComplete(completionContext, this, enumValueDefinition!.GetDirectives());
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
