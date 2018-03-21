using System;
using System.Collections.Generic;
using System.Linq;

namespace Prometheus.Types
{
    public class InterfaceType
        : IOutputType
    {
        private readonly InterfaceTypeConfig _config;
        private IReadOnlyDictionary<string, Field> _fields;

        public InterfaceType(InterfaceTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An interface type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            Name = config.Name;
            Description = config.Description;
        }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, Field> Fields
        {
            get
            {
                if (_fields == null)
                {
                    var fields = _config.Fields();
                    if (fields == null)
                    {
                        throw new InvalidOperationException(
                            "The fields collection mustn't be null.");
                    }
                    _fields = fields.ToDictionary(t => t.Name);
                }
                return _fields;
            }
        }
    }

    public class InterfaceTypeConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IEnumerable<Field>> Fields { get; set; }
    }
}