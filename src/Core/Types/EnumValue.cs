using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class EnumValue
         : ITypeSystemNode
    {
        public EnumValue(EnumValueConfig config)
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

    public class EnumValue<T>
        : EnumValue
    {
        public EnumValue(EnumValueConfig<T> config)
            : base(config)
        {
        }
    }

    public class EnumValueConfig
    {
        private object _value;

        public string Name { get; set; }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

        public virtual object Value
        {
            get => _value;
            set
            {
                _value = value;
                if (string.IsNullOrEmpty(Name))
                {
                    Name = value.ToString().ToUpperInvariant();
                }
            }
        }
    }

    public class EnumValueConfig<T>
        : EnumValueConfig
    {
        public new T Value
        {
            get => (T)base.Value;
            set => base.Value = value;
        }
    }
}
