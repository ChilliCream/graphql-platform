using System;
using System.Linq;
using Xunit;

namespace HotChocolate.AspNetCore.Serialization
{
    public class VariablePathTests
    {
        [Fact]
        public void PathHasNoFields()
        {
            // arrange
            const string path = "variables";

            // act
            void Parse() => VariablePath.Parse(path);

            // assert
            Assert.Equal(
                ErrorCodes.Server.MultiPartInvalidPath,
                Assert.Throws<GraphQLRequestException>(Parse).Errors.Single().Code);
        }

        [Fact]
        public void InvalidRoot()
        {
            // arrange
            const string path = "foo.bar";

            // act
            void Parse() => VariablePath.Parse(path);

            // assert
            Assert.Equal(
                ErrorCodes.Server.MultiPartPathMustStartWithVariable,
                Assert.Throws<GraphQLRequestException>(Parse).Errors.Single().Code);
        }

        [Fact]
        public void PathMustStartWithKey()
        {
            // arrange
            const string path = "variables.1.foo.bar";

            // act
            void Parse() => VariablePath.Parse(path);

            // assert
            Assert.Throws<InvalidOperationException>(Parse);
        }

        [Fact]
        public void ValidPath()
        {
            // arrange
            const string s = "variables.foo.bar.1.baz";

            // act
            var path = VariablePath.Parse(s);

            // assert
            Assert.Equal("foo", path.Key.Value);
            Assert.Equal("bar", Assert.IsType<KeyPathSegment>(path.Key.Next).Value);
            Assert.Equal(1, Assert.IsType<IndexPathSegment>(path.Key.Next.Next).Value);
            Assert.Equal("baz", Assert.IsType<KeyPathSegment>(path.Key.Next.Next.Next).Value);
        }
    }
}
