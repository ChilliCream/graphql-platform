using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Introspection
{
    public class Foo
        : ObjectType
    {
        public Foo()
            : base(Create())
        {
        }

        private static ObjectTypeConfig Create()
        {
            return new ObjectTypeConfig
            {

            }
        }
    }
}
