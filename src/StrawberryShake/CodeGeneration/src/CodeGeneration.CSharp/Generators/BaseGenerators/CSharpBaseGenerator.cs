using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract class CSharpBaseGenerator<T> : CodeGenerator<T> where T : ICodeDescriptor
    {
        protected void AssertNonNull(CodeWriter writer, T descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }
        }
    }
}
