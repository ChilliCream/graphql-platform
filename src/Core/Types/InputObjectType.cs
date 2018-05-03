using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputObjectType
        : IInputType
        , INamedType
        , INullableType
    {
        private readonly InputObjectTypeConfig _config;
        public Dictionary<string, InputField> _fields;

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

        public IReadOnlyDictionary<string, InputField> Fields
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

        public Type NativeType => throw new NotImplementedException();

        public bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public object ParseLiteral(IValueNode literal)
        {
            throw new NotImplementedException();
        }
    }

    public class InputObjectTypeConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IEnumerable<InputField>> Fields { get; }
    }
}
