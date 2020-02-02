using System;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    public class SortOperation
    {
        public SortOperation(PropertyInfo property) : this(property, false)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }
        public SortOperation(PropertyInfo property, bool isObject)
        {
            IsObject = isObject;
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }

        public bool IsObject { get; }

        public PropertyInfo Property { get; }
    }
}
