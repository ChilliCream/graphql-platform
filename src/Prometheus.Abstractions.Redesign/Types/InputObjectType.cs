using System;
using System.Collections.Generic;
using System.Linq;

namespace Prometheus.Types
{
    public class InputObjectType
        : IOutputType
        , INamedType
        , INullableType
    {
        private readonly InputObjectTypeConfig _config;
        public Dictionary<string, InputValue> _fields;

        public InputObjectType(InputObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An input object type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            Name = _config.Name;
            Description = _config.Description;
        }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, InputValue> Fields
        {
            get
            {
                if (_fields == null)
                {
                    var fields = _config.Fields();
                    if (fields == null || !fields.Any())
                    {
                        throw new InvalidOperationException(
                            "An input object type must at least have one field.");
                    }
                    _fields = fields.ToDictionary(t => t.Name);
                }
                return _fields;
            }
        }
    }

    public class InputObjectTypeConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IEnumerable<InputValue>> Fields { get; }
    }
}