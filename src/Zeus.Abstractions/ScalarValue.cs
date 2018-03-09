using System;
using Newtonsoft.Json;

namespace Zeus.Abstractions
{
    public class ScalarValue<TValue>
        : IValue
        , IScalarValue
    {
        protected ScalarValue(TValue value, NamedType type)
        {
            if (object.Equals(value, null))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if(!type.IsScalarType())
            {
                throw new ArgumentException(
                    "The type must be as scalar type.", 
                    nameof(type));
            }

            Value = value;
            Type = type;
        }

        public TValue Value { get; }

        public NamedType Type { get; }
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(Value); // TODO : get rid of newtonsoft in abstractions
        }

        object IValue.Value => Value;
    }
}