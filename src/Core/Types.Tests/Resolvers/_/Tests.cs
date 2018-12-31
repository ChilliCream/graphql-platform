using System.Linq.Expressions;
using Moq;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class Tests
    {
        [Fact]
        public void GetArgument()
        {
            // arrange
            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Argument<int>("fooBar")).Returns(123);

            var fooBar = Expression.Constant(new FooBar());
            var helper = new ResolverCompiler<IResolverContext>();
            var test = helper.CreateResolver(fooBar, typeof(FooBar).GetMethod("GetFooBar"));

            var x = test(context.Object);

            Assert.Equal(123, x);
        }

        [Fact]
        public void Prop()
        {
            // arrange
            var context = new Mock<IResolverContext>(MockBehavior.Strict);

            var fooBar = Expression.Constant(new FooBar());
            var helper = new ResolverCompiler<IResolverContext>();
            var test = helper.CreateResolver(fooBar, typeof(FooBar).GetProperty("Prop"));

            var x = test(context.Object);

            Assert.Equal("FooBar", x);
        }

        public class FooBar
        {
            public int GetFooBar(int fooBar)
            {
                return fooBar;
            }

            public string Prop => "FooBar";
        }
    }
}
