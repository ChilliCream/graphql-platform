using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public delegate string SerializeValue(object value);
    public delegate object ParseLiteral(IValueNode value, GetVariableValue getVariableValue);
    public delegate object GetVariableValue(string variableName);

    public class ScalarType
        : IOutputType
        , IInputType
        , INamedType
        , INullableType
    {
        private readonly ScalarTypeConfig _config;
        private readonly SerializeValue _serialize;
        private readonly ParseLiteral _parseLiteral;

        public ScalarType(ScalarTypeConfig config)
        {
            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A scalar type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            Name = config.Name;
            Description = config.Description;
            _serialize = config.Serialize;
            _parseLiteral = config.ParseLiteral;
        }

        public string Name { get; }

        public string Description { get; }

        // .net native to external  
        public string Serialize(object value)
        {
            return _serialize(value);
        }

        // ast node to .net native
        public object ParseLiteral(IValueNode value, GetVariableValue getVariableValue)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (getVariableValue == null)
            {
                throw new ArgumentNullException(nameof(getVariableValue));
            }

            return _parseLiteral(value, getVariableValue);
        }
    }

    public class ScalarTypeConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public SerializeValue Serialize { get; set; }
        
        public ParseLiteral ParseLiteral { get; set; }
    }
}