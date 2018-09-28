using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace HotChocolate.Language
{
    public class JsonParserTests
    {
        [Fact]
        public void ParseObject()
        {
            // arrange
            string json = "{ \"a\":\"a\" \"b\": { \"a\":\"a\" } }";

            // act
            IValueNode node = Parser.Default.ParseJson(json);

            // assert
            Assert.IsType<ObjectValueNode>(node);
            Assert.Collection(((ObjectValueNode)node).Fields,
                t =>
                {
                    Assert.Equal("a", t.Name.Value);
                    Assert.Equal("a", ((StringValueNode)t.Value).Value);
                },
                t =>
                {
                    Assert.Equal("b", t.Name.Value);
                    Assert.Collection(((ObjectValueNode)t.Value).Fields,
                        x =>
                        {
                            Assert.Equal("a", x.Name.Value);
                            Assert.Equal("a", ((StringValueNode)x.Value).Value);
                        });
                });
        }

        [Fact]
        public void ParseJson5Object()
        {
            // arrange
            string json = "{ a:\"a\" b: { a:\"a\" } }";

            // act
            IValueNode node = Parser.Default.ParseJson(json);

            // assert
            Assert.IsType<ObjectValueNode>(node);
            Assert.Collection(((ObjectValueNode)node).Fields,
                t =>
                {
                    Assert.Equal("a", t.Name.Value);
                    Assert.Equal("a", ((StringValueNode)t.Value).Value);
                },
                t =>
                {
                    Assert.Equal("b", t.Name.Value);
                    Assert.Collection(((ObjectValueNode)t.Value).Fields,
                        x =>
                        {
                            Assert.Equal("a", x.Name.Value);
                            Assert.Equal("a", ((StringValueNode)x.Value).Value);
                        });
                });
        }

        [Fact]
        public void ParseObjectWithList()
        {
            // arrange
            string json = "{ \"a\":\"a\" \"b\": [ { \"a\":\"a\" } ] }";

            // act
            IValueNode node = Parser.Default.ParseJson(json);

            // assert
            Assert.IsType<ObjectValueNode>(node);
            Assert.Collection(((ObjectValueNode)node).Fields,
                t =>
                {
                    Assert.Equal("a", t.Name.Value);
                    Assert.Equal("a", ((StringValueNode)t.Value).Value);
                },
                t =>
                {
                    Assert.Equal("b", t.Name.Value);
                    Assert.Collection(((ListValueNode)t.Value).Items,
                        x =>
                        {
                            Assert.IsType<ObjectValueNode>(x);
                            Assert.Collection(((ObjectValueNode)x).Fields,
                                y =>
                                {
                                    Assert.Equal("a", y.Name.Value);
                                    Assert.Equal("a", ((StringValueNode)y.Value).Value);
                                });
                        });
                });
        }

        [Fact]
        public void ParseObjectWithNestedList()
        {
            // arrange
            string json = "{ \"a\":\"a\" \"b\": [ [ { \"a\":\"a\" } ] ] }";

            // act
            IValueNode node = Parser.Default.ParseJson(json);

            // assert
            Assert.IsType<ObjectValueNode>(node);
            Assert.Collection(((ObjectValueNode)node).Fields,
                t =>
                {
                    Assert.Equal("a", t.Name.Value);
                    Assert.Equal("a", ((StringValueNode)t.Value).Value);
                },
                t =>
                {
                    Assert.Equal("b", t.Name.Value);
                    Assert.Collection(((ListValueNode)t.Value).Items,
                        x =>
                        {
                            Assert.Collection(((ListValueNode)x).Items,
                            y =>
                            {
                                Assert.IsType<ObjectValueNode>(y);
                                Assert.Collection(((ObjectValueNode)y).Fields,
                                    z =>
                                    {
                                        Assert.Equal("a", z.Name.Value);
                                        Assert.Equal("a", ((StringValueNode)z.Value).Value);
                                    });
                            });
                        });
                });
        }

        [Fact]
        public void ParseStringValue()
        {
            // arrange
            string json = "\"a\"";

            // act
            IValueNode node = Parser.Default.ParseJson(json);

            // assert
            Assert.IsType<StringValueNode>(node);
            Assert.Equal("a", ((StringValueNode)node).Value);
        }

        [Fact]
        public void ParseIntValue()
        {
            // arrange
            string json = "1";

            // act
            IValueNode node = Parser.Default.ParseJson(json);

            // assert
            Assert.IsType<IntValueNode>(node);
            Assert.Equal("1", ((IntValueNode)node).Value);
        }

        [Fact]
        public void ParseFloatValue()
        {
            // arrange
            string json = "1.0";

            // act
            IValueNode node = Parser.Default.ParseJson(json);

            // assert
            Assert.IsType<FloatValueNode>(node);
            Assert.Equal("1.0", ((FloatValueNode)node).Value);
        }

        [Fact]
        public void ParseNullValue()
        {
            // arrange
            string json = "null";

            // act
            IValueNode node = Parser.Default.ParseJson(json);

            // assert
            Assert.IsType<NullValueNode>(node);
        }

        [Fact]
        public void ParseList()
        {
            // arrange
            string json = "[ 1, 2, 3 ]";

            // act
            IValueNode node = Parser.Default.ParseJson(json);

            // assert
            Assert.IsType<ListValueNode>(node);
            Assert.Collection(((ListValueNode)node).Items,
                t =>
                {
                    Assert.IsType<IntValueNode>(t);
                    Assert.Equal("1", ((IntValueNode)t).Value);

                },
                t =>
                {
                    Assert.IsType<IntValueNode>(t);
                    Assert.Equal("2", ((IntValueNode)t).Value);

                },
                t =>
                {
                    Assert.IsType<IntValueNode>(t);
                    Assert.Equal("3", ((IntValueNode)t).Value);

                });
        }
    }
}
