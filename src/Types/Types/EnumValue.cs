using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class EnumValue
         : ITypeSystemNode
    {
        internal EnumValue(EnumValueDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (descriptor.Value == null)
            {
                throw new ArgumentException(
                    "The inner value of enum value cannot be null or empty.",
                    nameof(descriptor));
            }

            SyntaxNode = descriptor.SyntaxNode;
            Name = string.IsNullOrEmpty(descriptor.Name)
                ? descriptor.Value.ToString()
                : descriptor.Name;
            Description = descriptor.Description;
            DeprecationReason = descriptor.DeprecationReason;
            IsDeprecated = !string.IsNullOrEmpty(descriptor.DeprecationReason);
            Value = descriptor.Value;
        }

        public EnumValueDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public string DeprecationReason { get; }

        public bool IsDeprecated { get; }

        public object Value { get; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            yield break;
        }
    }
}
