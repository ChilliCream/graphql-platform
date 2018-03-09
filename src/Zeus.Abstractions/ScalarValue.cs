using System;
using Newtonsoft.Json;

namespace Zeus.Abstractions
{
    public class ScalarValue<TValue>
        : IValue
    {
        protected ScalarValue(TValue value)
        {
            if (object.Equals(value, null))
            {
                throw new ArgumentNullException(nameof(value));
            }

            Value = value;
        }

        public TValue Value { get; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(Value); // TODO : get rid of newtonsoft in abstractions
        }
    }
}