using System;
using System.Collections;
using HotChocolate.Types;

namespace HotChocolate.Execution.ValueConverters
{
    internal class ListValueConverter
        : IInputValueConverter
    {
        public bool CanConvert(IInputType inputType)
        {
            return inputType is ListType;
        }

        public bool TryConvert(Type from, Type to, object value, out object convertedValue)
        {
            if (value == null)
            {
                convertedValue = null;
                return true;
            }

            if (from.IsArray)
            {
                if (to.IsAssignableFrom(from))
                {
                    convertedValue = value;
                    return true;
                }

                if (to.IsClass && typeof(IList).IsAssignableFrom(to))
                {
                    Array array = (Array)value;
                    IList list = (IList)Activator.CreateInstance(to);
                    foreach (object o in array)
                    {
                        list.Add(o);
                    }
                    convertedValue = list;
                    return true;
                }
            }

            convertedValue = null;
            return true;
        }
    }
}
