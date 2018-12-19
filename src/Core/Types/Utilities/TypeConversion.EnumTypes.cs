using System;
using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Utilities
{
    public partial class TypeConversion
    {
        private bool TryCreateEnumConverter(
            Type from, Type to, out ChangeType converter)
        {
            if (from == typeof(string) && to.IsEnum)
            {
                converter = source => Enum.Parse(to, (string)source, true);
                Register(from, to, converter);
                return true;
            }

            converter = null;
            return false;
        }
    }
}
