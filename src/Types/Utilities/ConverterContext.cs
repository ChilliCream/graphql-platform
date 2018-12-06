using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal class ConverterContext
    {
        public object Object { get; set; }

        public Type ClrType { get; set; }

        public IInputType InputType { get; set; }

        public FieldCollection<InputField> InputFields { get; set; }

        public ILookup<string, PropertyInfo> Fields { get; set; }
    }
}
