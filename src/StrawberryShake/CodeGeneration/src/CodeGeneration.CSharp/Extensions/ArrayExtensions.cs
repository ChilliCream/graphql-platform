using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    internal static class ArrayExtensions
    {
        public static ArrayBuilder ForEach<T>(
            this ArrayBuilder arrayBuilder,
            IEnumerable<T> enumerable,
            Action<ArrayBuilder, T> configure)
        {
            foreach (T element in enumerable)
            {
                configure(arrayBuilder, element);
            }

            return arrayBuilder;
        }
    }
}
