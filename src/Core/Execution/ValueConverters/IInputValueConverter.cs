using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Execution.ValueConverters
{
    public interface IInputValueConverter
    {
        bool CanConvert(IInputType inputType);

        bool TryConvert(Type from, Type to, object value, out object convertedValue);
    }
}
