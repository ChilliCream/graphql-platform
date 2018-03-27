using System;

namespace Prometheus.Types
{
    public class EnumValue
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
    }

    public class EnumValueConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

        public object Value { get; set; }
    }
}