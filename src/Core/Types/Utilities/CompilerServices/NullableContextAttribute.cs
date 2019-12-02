using System;

namespace HotChocolate.Utilities.CompilerServices
{
    [AttributeUsage(
        AttributeTargets.Module |
        AttributeTargets.Class |
        AttributeTargets.Delegate |
        AttributeTargets.Interface |
        AttributeTargets.Method |
        AttributeTargets.Struct,
        AllowMultiple = false,
        Inherited = false)]
    internal sealed class NullableContextAttribute : Attribute
    {
        public readonly byte Flag;

        public NullableContextAttribute(byte flag)
        {
            Flag = flag;
        }
    }
}
