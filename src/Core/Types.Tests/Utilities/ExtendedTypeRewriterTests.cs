using System;
using Xunit;

namespace HotChocolate.Utilities
{
    public class ExtendedTypeRewriterTests
    {
        [Fact]
        public void Test()
        {
            IExtendedType type = typeof(Foo).GetMethod("GetBar").GetExtendeMethodTypeInfo().ReturnType;

        }

        public class Foo
        {
            public byte[] GetBar()
            {
                throw new NotImplementedException();
            }
        }
    }
}
