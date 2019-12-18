#nullable enable

using System;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class ExtendObjectTypeAttribute : Attribute
    {
        public ExtendObjectTypeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
