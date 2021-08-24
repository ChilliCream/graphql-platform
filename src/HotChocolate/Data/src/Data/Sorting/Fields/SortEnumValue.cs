using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public sealed class SortEnumValue
        : ISortEnumValue
    {
        private readonly DirectiveCollection _directives;

        public SortEnumValue(
            ITypeCompletionContext completionContext,
            SortEnumValueDefinition enumValueDefinition)
        {
            if (completionContext == null)
            {
                throw new ArgumentNullException(nameof(completionContext));
            }

            if (enumValueDefinition is null)
            {
                throw new ArgumentNullException(nameof(enumValueDefinition));
            }

            if (enumValueDefinition.Value is null)
            {
                throw new ArgumentException(
                    DataResources.SortEnumValue_ValueIsNull,
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
            Handler = enumValueDefinition.Handler;
            Operation = enumValueDefinition.Operation;

            _directives = new DirectiveCollection(this, enumValueDefinition!.GetDirectives());
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

        public ISortOperationHandler Handler { get; }

        public int Operation { get; }
    }
}
