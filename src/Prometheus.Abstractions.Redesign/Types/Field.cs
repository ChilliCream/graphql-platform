using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Prometheus.Resolvers;

namespace Prometheus.Types
{
    public class Field
    {
        private readonly FieldConfig _config;
        private IOutputType _type;
        private IReadOnlyDictionary<string, InputField> _arguments;
        private FieldResolverDelegate _resolver;

        public Field(FieldConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A field name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            Name = config.Name;
            Description = config.Description;
        }

        public string Name { get; }

        public string Description { get; }

        public IOutputType Type
        {
            get
            {
                if (_type == null)
                {
                    _type = _config.Type();
                    if (_type == null)
                    {
                        throw new InvalidOperationException(
                            "The field return type mustn't be null.");
                    }
                }
                return _type;
            }
        }

        public IReadOnlyDictionary<string, InputField> Arguments
        {
            get
            {
                if (_arguments == null)
                {
                    var arguments = _config.Arguments();
                    _arguments = (arguments == null)
                        ? new Dictionary<string, InputField>()
                        : _config.Arguments().ToDictionary(t => t.Name);
                }
                return _arguments;
            }
        }

        public FieldResolverDelegate Resolver
        {
            get
            {
                if (_resolver == null)
                {
                    _resolver = _config.Resolver();
                }
                return _resolver;
            }
        }
    }

    public class FieldConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IOutputType> Type { get; set; }

        public Func<IEnumerable<InputField>> Arguments { get; set; }

        public Func<FieldResolverDelegate> Resolver { get; set; }
    }
}