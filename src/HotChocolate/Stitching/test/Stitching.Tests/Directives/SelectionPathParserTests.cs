using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace HotChocolate.Stitching
{
    public class SelectionPathParserTests
    {
        [Fact]
        public void Parse_Single_Chain_No_Arguments()
        {
            // arrange
            var pathString = "foo";

            // act
            IImmutableStack<SelectionPathComponent> path =
                SelectionPathParser.Parse(pathString);

            // assert
            Assert.Collection(path,
                segment =>
                {
                    Assert.Equal("foo", segment.Name.Value);
                    Assert.Empty(segment.Arguments);
                });
        }

        [Fact]
        public void Parse_Single_Chain_With_Literal()
        {
            // arrange
            var pathString = "foo(bar: 1)";

            // act
            IImmutableStack<SelectionPathComponent> path =
                SelectionPathParser.Parse(pathString);

            // assert
            Assert.Collection(path,
                segment =>
                {
                    Assert.Equal("foo", segment.Name.Value);
                    Assert.Collection(segment.Arguments,
                        argument =>
                        {
                            Assert.Equal("bar", argument.Name.Value);
                            Assert.Equal("1", argument.Value.Value);
                        });
                });
        }

        [Fact]
        public void Parse_Two_Chain_No_Arguments()
        {
            // arrange
            var pathString = "foo.bar";

            // act
            IImmutableStack<SelectionPathComponent> path =
                SelectionPathParser.Parse(pathString);

            // assert
            Assert.Collection(path.Reverse(),
                segment =>
                {
                    Assert.Equal("foo", segment.Name.Value);
                    Assert.Empty(segment.Arguments);
                },
                segment =>
                {
                    Assert.Equal("bar", segment.Name.Value);
                    Assert.Empty(segment.Arguments);
                });
        }

        [Fact]
        public void Parse_Two_Chain_With_Literal()
        {
            // arrange
            var pathString = "foo(bar: 1).baz(quox: 2)";

            // act
            IImmutableStack<SelectionPathComponent> path =
                SelectionPathParser.Parse(pathString);

            // assert
            Assert.Collection(path.Reverse(),
                segment =>
                {
                    Assert.Equal("foo", segment.Name.Value);
                    Assert.Collection(segment.Arguments,
                        argument =>
                        {
                            Assert.Equal("bar", argument.Name.Value);
                            Assert.Equal("1", argument.Value.Value);
                        });
                },
                segment =>
                {
                    Assert.Equal("baz", segment.Name.Value);
                    Assert.Collection(segment.Arguments,
                        argument =>
                        {
                            Assert.Equal("quox", argument.Name.Value);
                            Assert.Equal("2", argument.Value.Value);
                        });
                });
        }

        [Fact]
        public void Parse_Single_Chain_With_ScopedVariable()
        {
            // arrange
            var pathString = "foo(bar: $fields:foo)";

            // act
            IImmutableStack<SelectionPathComponent> path =
                SelectionPathParser.Parse(pathString);

            // assert
            Assert.Collection(path,
                segment =>
                {
                    Assert.Equal("foo", segment.Name.Value);
                    Assert.Collection(segment.Arguments,
                        argument =>
                        {
                            Assert.Equal("bar", argument.Name.Value);

                            ScopedVariableNode variable =
                                Assert.IsType<ScopedVariableNode>(argument.Value);

                            Assert.Equal("fields", variable.Scope.Value);
                            Assert.Equal("foo", variable.Name.Value);
                        });
                });
        }

        [Fact]
        public void Parse_Two_Chain_With_ScopedVariable()
        {
            // arrange
            var pathString = "foo(bar: $fields:foo).baz(quox: 1)";

            // act
            IImmutableStack<SelectionPathComponent> path =
                SelectionPathParser.Parse(pathString);

            // assert
            Assert.Collection(path.Reverse(),
                segment =>
                {
                    Assert.Equal("foo", segment.Name.Value);
                    Assert.Collection(segment.Arguments,
                        argument =>
                        {
                            Assert.Equal("bar", argument.Name.Value);

                            ScopedVariableNode variable =
                                Assert.IsType<ScopedVariableNode>(argument.Value);

                            Assert.Equal("fields", variable.Scope.Value);
                            Assert.Equal("foo", variable.Name.Value);
                        });
                },
                segment =>
                {
                    Assert.Equal("baz", segment.Name.Value);
                    Assert.Collection(segment.Arguments,
                        argument =>
                        {
                            Assert.Equal("quox", argument.Name.Value);
                            Assert.Equal("1", argument.Value.Value);
                        });
                });
        }
    }
}
