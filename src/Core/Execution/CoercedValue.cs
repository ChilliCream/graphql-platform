using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public class CoercedValue
    {
        public CoercedValue(IInputType inputType, IValueNode value)
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            InputType = inputType;
            Value = value;
        }

        public IInputType InputType { get; }
        public IValueNode Value { get; }
    }
}
