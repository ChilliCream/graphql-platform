using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class DefaultTypeInspectorTests
    {
        [Fact]
        public void Discover_Property_That_Returns_Object_And_Has_TypeAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(typeof(ObjectPropWithTypeAttribute)).ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        [Fact]
        public void Discover_Property_That_Returns_Object_And_Has_DescriptorAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(typeof(ObjectPropWithDescriptorAttribute)).ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        [Fact]
        public void Discover_Method_That_Returns_Object_And_Has_TypeAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(typeof(ObjectMethodWithTypeAttribute)).ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        [Fact]
        public void Discover_Method_That_Returns_Object_And_Has_DescriptorAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(typeof(ObjectMethodWithDescriptorAttribute)).ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        [Fact]
        public void Discover_Method_With_Object_Parameter_And_Has_TypeAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(
                    typeof(MethodAndObjectParameterWithTypeAttribute)).ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        [Fact]
        public void Discover_Method_With_Object_Parameter_And_Has_DescriptorAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(
                    typeof(MethodAndObjectParameterWithDescriptorAttribute)).ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        [Fact]
        public void Discover_Method_That_Returns_TaskObject_And_Has_TypeAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(typeof(TaskObjectMethodWithTypeAttribute)).ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        [Fact]
        public void Discover_Method_That_Returns_TaskObject_And_Has_DescriptorAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(typeof(TaskObjectMethodWithDescriptorAttribute)).ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        [Fact]
        public void Discover_Method_That_Returns_ValueTaskObject_And_Has_TypeAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(typeof(ValueTaskObjectMethodWithTypeAttribute)).ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        [Fact]
        public void Discover_Method_That_Returns_ValueTaskObject_And_Has_DescriptorAttribute()
        {
            // arrange
            var typeInspector = new DefaultTypeInspector();

            // act
            MemberInfo[] discovered =
                typeInspector.GetMembers(
                    typeof(ValueTaskObjectMethodWithDescriptorAttribute))
                    .ToArray();

            // assert
            Assert.Collection(discovered,
                p => Assert.Equal("ShouldBeFound", p.Name));
        }

        public class ObjectPropWithTypeAttribute
        {
            public object ShouldNotBeFound { get; }

            [GraphQLType(typeof(StringType))]
            public object ShouldBeFound { get; }
        }

        public class ObjectPropWithDescriptorAttribute
        {
            public object ShouldNotBeFound { get; }

            [SomeAttribute]
            public object ShouldBeFound { get; }
        }

        public class ObjectMethodWithTypeAttribute
        {
            public object ShouldNotBeFound() => null;

            [GraphQLType(typeof(StringType))]
            public object ShouldBeFound() => null;
        }

        public class ObjectMethodWithDescriptorAttribute
        {
            public object ShouldNotBeFound() => null;

            [SomeAttribute]
            public object ShouldBeFound() => null;
        }

        public class MethodAndObjectParameterWithTypeAttribute
        {
            public string ShouldNotBeFound(
                object o) => null;


            public string ShouldBeFound(
                [GraphQLType(typeof(StringType))]
                object o) => null;
        }

        public class MethodAndObjectParameterWithDescriptorAttribute
        {
            public string ShouldNotBeFound(
                object o) => null;


            public string ShouldBeFound(
                [SomeAttribute]
                object o) => null;
        }

        public class TaskObjectMethodWithTypeAttribute
        {
            public Task<object> ShouldNotBeFound() => null;

            [GraphQLType(typeof(StringType))]
            public Task<object> ShouldBeFound() => null;
        }

        public class TaskObjectMethodWithDescriptorAttribute
        {
            public Task<object> ShouldNotBeFound() => null;

            [SomeAttribute]
            public Task<object> ShouldBeFound() => null;
        }

         public class ValueTaskObjectMethodWithTypeAttribute
        {
            public ValueTask<object> ShouldNotBeFound() => default;

            [GraphQLType(typeof(StringType))]
            public ValueTask<object> ShouldBeFound() => default;
        }

        public class ValueTaskObjectMethodWithDescriptorAttribute
        {
            public ValueTask<object> ShouldNotBeFound() => default;

            [SomeAttribute]
            public ValueTask<object> ShouldBeFound() => default;
        }

        public sealed class SomeAttribute
            : DescriptorAttribute
        {
            protected internal override void TryConfigure(
                IDescriptorContext context,
                IDescriptor descriptor,
                ICustomAttributeProvider element)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
