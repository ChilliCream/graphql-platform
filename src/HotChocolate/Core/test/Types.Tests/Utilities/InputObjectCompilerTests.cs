
using System.Collections.Generic;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities
{
    public class InputObjectCompilerTests
    {
        [Fact]
        public void Convert_ObjectWithPropertiesDifferingByCase()
        {
            void Action() => SchemaBuilder.New()
                .AddType<InputObjectType<Foo>>()
                .Create();

            Assert.Throws<SchemaException>(Action);
        }

        private class Foo
        {
            public string DifferOnlyByCase { get; set; }

            public string DifferONLYByCase { get; set; }
        }
    }
}
