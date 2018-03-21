using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Prometheus.Types
{
    public class ObjectType
        : IOutputType
    {
        private readonly ObjectTypeConfig _config;
        private IReadOnlyDictionary<string, InterfaceType> _interfaces;
        private IReadOnlyDictionary<string, Field> _fields;

        public ObjectType(ObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            Name = config.Name;
            Description = config.Description;
        }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, InterfaceType> Interfaces
        {
            get
            {
                if (_interfaces == null)
                {
                    var interfaces = _config.Interfaces();
                    _interfaces = (interfaces == null)
                        ? new Dictionary<string, InterfaceType>()
                        : interfaces.ToDictionary(t => t.Name);
                }
                return _interfaces;
            }
        }

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

    public class ObjectTypeConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IEnumerable<InterfaceType>> Interfaces { get; set; }

        public Func<IEnumerable<Field>> Fields { get; set; }
    }
}