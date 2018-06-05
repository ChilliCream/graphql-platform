using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class EnumValue
         : ITypeSystemNode
    {
        internal EnumValue(EnumValueConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A enum value name must not be null or empty.",
                    nameof(config));
            }

            if (config.Value == null)
            {
                throw new ArgumentException(
                    "The inner value of enum value cannot be null or empty.",
                    nameof(config));
            }

            Name = config.Name;
            Description = config.Description;
            DeprecationReason = config.DeprecationReason;
            IsDeprecated = !string.IsNullOrEmpty(config.DeprecationReason);
            Value = config.Value;
        }

        public string Name { get; }

        public string Description { get; }

        public string DeprecationReason { get; }

        public bool IsDeprecated { get; }

        public object Value { get; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => throw new NotImplementedException();

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            yield break;
        }
    }
}
