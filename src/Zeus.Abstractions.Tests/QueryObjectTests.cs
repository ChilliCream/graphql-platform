using System;
using System.Linq;
using Xunit;

namespace Zeus.Abstractions.Tests
{
    public class QueryObjectTests
    {
        [Fact]
        public void FieldTest()
        {
            // act
            Field field = new Field("foo");

            // assert
            Assert.Equal("foo", field.Name);
            Assert.Null(field.Alias);
            Assert.False(field.Arguments.Any());
            Assert.False(field.Directives.Any());
            Assert.Null(field.SelectionSet);

            string expectedStringRepresentation = $"foo";
            Assert.Equal(expectedStringRepresentation, field.ToString());
        }

        [Fact]
        public void FieldWithAliasTest()
        {
            // act
            Field field = new Field("a", "foo",
                Enumerable.Empty<Argument>(),
                Enumerable.Empty<Directive>(),
                null);

            // assert
            Assert.Equal("foo", field.Name);
            Assert.Equal("a", field.Alias);
            Assert.False(field.Arguments.Any());
            Assert.False(field.Directives.Any());
            Assert.Null(field.SelectionSet);

            string expectedStringRepresentation = $"a: foo";
            Assert.Equal(expectedStringRepresentation, field.ToString());
        }

        [Fact]
        public void FieldWithAliasAndSelections()
        {
            // act
            Field field = new Field("a", "foo",
                Enumerable.Empty<Argument>(),
                Enumerable.Empty<Directive>(),
                new ISelection[] { new Field("b") });

            // assert
            Assert.Equal("foo", field.Name);
            Assert.Equal("a", field.Alias);
            Assert.False(field.Arguments.Any());
            Assert.False(field.Directives.Any());
            Assert.Equal(1, field.SelectionSet.Count);

            string expectedStringRepresentation = $"a: foo{Environment.NewLine}{{{Environment.NewLine}  b{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, field.ToString());
        }

        [Fact]
        public void FieldWithSelections()
        {
            // act
            Field field = new Field(null, "foo",
                Enumerable.Empty<Argument>(),
                Enumerable.Empty<Directive>(),
                new ISelection[] { new Field("b") });

            // assert
            Assert.Equal("foo", field.Name);
            Assert.Null(field.Alias);
            Assert.False(field.Arguments.Any());
            Assert.False(field.Directives.Any());
            Assert.Equal(1, field.SelectionSet.Count);

            string expectedStringRepresentation = $"foo{Environment.NewLine}{{{Environment.NewLine}  b{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, field.ToString());
        }

    }
}