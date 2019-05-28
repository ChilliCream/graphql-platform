using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class EnumValue
        : IHasDirectives
    {
        private EnumValueDefinition _definition;
        private Dictionary<string, object> _contextData;

        public EnumValue(EnumValueDefinition enumValueDefinition)
        {
            if (enumValueDefinition == null)
            {
                throw new ArgumentNullException(nameof(enumValueDefinition));
            }

            if (enumValueDefinition.Value == null)
            {
                throw new ArgumentException(
                    TypeResources.EnumValue_ValueIsNull,
                    nameof(enumValueDefinition));
            }

            _definition = enumValueDefinition;
            SyntaxNode = enumValueDefinition.SyntaxNode;
            Name = enumValueDefinition.Name.HasValue
                ? enumValueDefinition.Name
                : (NameString)enumValueDefinition.Value.ToString();
            Description = enumValueDefinition.Description;
            DeprecationReason = enumValueDefinition.DeprecationReason;
            IsDeprecated = !string.IsNullOrEmpty(
                enumValueDefinition.DeprecationReason);
            Value = enumValueDefinition.Value;
        }

        public EnumValueDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public string DeprecationReason { get; }

        public bool IsDeprecated { get; }

        public object Value { get; }

        public IDirectiveCollection Directives { get; private set; }

        public IReadOnlyDictionary<string, object> ContextData =>
            _contextData;

        internal void CompleteValue(ICompletionContext context)
        {
            var directives = new DirectiveCollection(
                this, _definition.Directives);
            directives.CompleteCollection(context);
            Directives = directives;

            OnCompleteValue(context, _definition);

            _contextData = new Dictionary<string, object>(
                _definition.ContextData);
            _definition = null;
        }

        protected virtual void OnCompleteValue(
            ICompletionContext context,
            EnumValueDefinition definition)
        {
        }
    }
}
