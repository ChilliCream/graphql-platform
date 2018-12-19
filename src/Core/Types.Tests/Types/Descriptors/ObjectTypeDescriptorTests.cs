using System.Linq;
using HotChocolate.Configuration;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectTypeDescriptorTests
    {
        [Fact]
        public void InferNameFromType()
        {
            // arrange
            var descriptor = new ObjectTypeDescriptor<Foo>();

            // act
            IObjectTypeDescriptor<Foo> desc = descriptor;

            // assert
            Assert.Equal("Foo", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void GetNameFromAttribute()
        {
            // arrange
            var descriptor = new ObjectTypeDescriptor<Foo2>();

            // act
            IObjectTypeDescriptor<Foo2> desc = descriptor;

            // assert
            Assert.Equal("FooAttr", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void OverwriteDefaultName()
        {
            // arrange
            var descriptor = new ObjectTypeDescriptor<Foo>();

            // act
            IObjectTypeDescriptor<Foo> desc = descriptor;
            desc.Name("FooBar");

            // assert
            Assert.Equal("FooBar", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void OverwriteAttributeName()
        {
            // arrange
            var descriptor = new ObjectTypeDescriptor<Foo2>();

            // act
            IObjectTypeDescriptor<Foo2> desc = descriptor;
            desc.Name("FooBar");

            // assert
            Assert.Equal("FooBar", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void InferFieldsFromType()
        {
            // arrange
            var descriptor = new ObjectTypeDescriptor<Foo>();

            // act
            IObjectTypeDescriptor<Foo> desc = descriptor;

            // assert
            Assert.Collection(
                descriptor.CreateDescription().Fields
                    .Select(t => t.Name)
                    .OrderBy(t => t),
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t),
                t => Assert.Equal("c", t),
                t => Assert.Equal("equals", t),
                t => Assert.Equal("hashCode", t));
        }

        [Fact]
        public void IgnoreOverridenPropertyField()
        {
            // arrange
            var descriptor = new ObjectTypeDescriptor<Foo>();

            // act
            IObjectTypeDescriptor<Foo> desc = descriptor;
            desc.Field(t => t.B).Ignore();

            // assert
            Assert.Collection(
                descriptor.CreateDescription().Fields
                    .Select(t => t.Name)
                    .OrderBy(t => t),
                t => Assert.Equal("a", t),
                t => Assert.Equal("c", t),
                t => Assert.Equal("equals", t),
                t => Assert.Equal("hashCode", t));
        }


        [Fact]
        public void IgnoreOverridenMethodField()
        {
            // arrange
            var descriptor = new ObjectTypeDescriptor<Foo>();

            // act
            IObjectTypeDescriptor<Foo> desc = descriptor;
            desc.Field(t => t.Equals(default)).Ignore();

            // assert
            Assert.Collection(
                descriptor.CreateDescription().Fields
                    .Select(t => t.Name)
                    .OrderBy(t => t),
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t),
                t => Assert.Equal("c", t),
                t => Assert.Equal("hashCode", t));
        }

        [Fact]
        public void DeclareFieldsExplicitly()
        {
            // arrange
            var descriptor = new ObjectTypeDescriptor<Foo>();

            // act
            IObjectTypeDescriptor<Foo> desc = descriptor;
            desc.Field(t => t.A);
            desc.BindFields(BindingBehavior.Explicit);

            // assert
            Assert.Collection(
               descriptor.CreateDescription().Fields.Select(t => t.Name),
               t => Assert.Equal("a", t));
        }

        public class Foo
            : FooBase
        {
            public string A { get; set; }
            public override string B { get; set; }
            public string C { get; set; }

            public override bool Equals(object obj) => true;

            public override int GetHashCode() => 0;
        }

        [GraphQLName("FooAttr")]
        public class Foo2
            : FooBase
        {
        }

        public class FooBase
        {
            public virtual string B { get; set; }
        }
    }
}
