using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Stitching
{
    public class SelectionPathParserTests
    {
        [Fact]
        public void Parse_PathWithoutArgs_ThreeComponentsFound()
        {
            // arrange
            var serializedPath = new Source("foo.bar.baz");

            // act
            Stack<SelectionPathComponent> path =
                SelectionPathParser.Parse(serializedPath);

            // assert
            Assert.Collection(path.Reverse(),
                t =>
                {
                    Assert.Equal("foo", t.Name.Value);
                    Assert.Empty(t.Arguments);
                },
                t =>
                {
                    Assert.Equal("bar", t.Name.Value);
                    Assert.Empty(t.Arguments);
                },
                t =>
                {
                    Assert.Equal("baz", t.Name.Value);
                    Assert.Empty(t.Arguments);
                });
        }

        [Fact]
        public void Parse_PathWithArgs_ThreeComponentsTwoWithArgs()
        {
            // arrange
            var serializedPath = new Source("foo(a: 1).bar.baz(b: \"s\")");

            // act
            Stack<SelectionPathComponent> path =
                SelectionPathParser.Parse(serializedPath);

            // assert
            Assert.Collection(path.Reverse(),
                c =>
                {
                    Assert.Equal("foo", c.Name.Value);
                    Assert.Collection(c.Arguments,
                        a =>
                        {
                            Assert.Equal("a", a.Name.Value);
                            Assert.IsType<IntValueNode>(a.Value);
                        });
                },
                c =>
                {
                    Assert.Equal("bar", c.Name.Value);
                    Assert.Empty(c.Arguments);
                },
                c =>
                {
                    Assert.Equal("baz", c.Name.Value);
                    Assert.Collection(c.Arguments,
                        a =>
                        {
                            Assert.Equal("b", a.Name.Value);
                            Assert.IsType<StringValueNode>(a.Value);
                        });
                });
        }

        [Fact]
        public void Parse_PathWithVarArgs_ThreeComponentsOneWithVarArgs()
        {
            // arrange
            var serializedPath = new Source("foo(a: $foo:bar).bar.baz");

            // act
            Stack<SelectionPathComponent> path =
                SelectionPathParser.Parse(serializedPath);

            // assert
            Assert.Collection(path.Reverse(),
                c =>
                {
                    Assert.Equal("foo", c.Name.Value);
                    Assert.Collection(c.Arguments,
                        a =>
                        {
                            Assert.Equal("a", a.Name.Value);
                            Assert.IsType<ScopedVariableNode>(a.Value);

                            var v = (ScopedVariableNode)a.Value;
                            Assert.Equal("foo_bar", v.ToVariableName());
                        });
                },
                c =>
                {
                    Assert.Equal("bar", c.Name.Value);
                    Assert.Empty(c.Arguments);
                },
                c =>
                {
                    Assert.Equal("baz", c.Name.Value);
                    Assert.Empty(c.Arguments);
                });
        }
    }
}
