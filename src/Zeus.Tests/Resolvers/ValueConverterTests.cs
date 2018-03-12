using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQLParser;
using Newtonsoft.Json;
using Xunit;
using Zeus.Execution;
using Zeus.Resolvers;
using Moq;
using System;
using Zeus.Abstractions;
using System.Linq;

namespace Zeus.Resolvers
{
    public class ValueConverterTests
    {
        [Fact]
        public void ConvertNullValue()
        {
            // arrange
            IValue value = NullValue.Instance;

            // act
            string s = ValueConverter.Convert<string>(value);

            // assert
            Assert.Null(s);
        }

        [Fact]
        public void ConvertStringValueToString()
        {
            // arrange
            IValue value = new StringValue("test");

            // act
            string s = ValueConverter.Convert<string>(value);

            // assert
            Assert.Equal("test", s);
        }

        [Fact]
        public void ConvertStringValueToInt()
        {
            // arrange
            IValue value = new StringValue("1");

            // act
            int i = ValueConverter.Convert<int>(value);

            // assert
            Assert.Equal(1, i);
        }

        [Fact]
        public void ConvertIntValueToInt()
        {
            // arrange
            IValue value = new IntegerValue(1);

            // act
            int i = ValueConverter.Convert<int>(value);

            // assert
            Assert.Equal(1, i);
        }

        [Fact]
        public void ConvertBooleanValueToBoolean()
        {
            // arrange
            IValue value = new BooleanValue(false);

            // act
            bool b = ValueConverter.Convert<bool>(value);

            // assert
            Assert.False(b);
        }

        [Fact]
        public void ConvertIListValueToList()
        {
            // arrange
            IValue value = new ListValue
            (
                new IValue[]
                {
                    new StringValue("s"),
                    new StringValue("a")
                }
            );

            // act
            IList<object> list = ValueConverter.Convert<IList<object>>(value);

            // assert
            Assert.NotNull(list);
            Assert.Collection(list,
                t => Assert.Equal("s", t),
                t => Assert.Equal("a", t));
        }

        [Fact]
        public void ConvertIListValueToListOfString()
        {
            // arrange
            IValue value = new ListValue
            (
                new IValue[]
                {
                    new StringValue("s"),
                    new StringValue("a")
                }
            );

            // act
            IList<string> list = ValueConverter.Convert<IList<string>>(value);

            // assert
            Assert.NotNull(list);
            Assert.Collection(list,
                t => Assert.Equal("s", t),
                t => Assert.Equal("a", t));
        }

        [Fact]
        public void ConvertCollectionValueToList()
        {
            // arrange
            IValue value = new ListValue
            (
                new IValue[]
                {
                    new StringValue("s"),
                    new StringValue("a")
                }
            );

            // act
            ICollection<object> list = ValueConverter.Convert<ICollection<object>>(value);

            // assert
            Assert.NotNull(list);
            Assert.Collection(list,
                t => Assert.Equal("s", t),
                t => Assert.Equal("a", t));
        }

        [Fact]
        public void ConvertListValueToList()
        {
            // arrange
            IValue value = new ListValue
            (
                new IValue[]
                {
                    new StringValue("s"),
                    new StringValue("a")
                }
            );

            // act
            List<object> list = ValueConverter.Convert<List<object>>(value);

            // assert
            Assert.NotNull(list);
            Assert.Collection(list,
                t => Assert.Equal("s", t),
                t => Assert.Equal("a", t));
        }

        [Fact]
        public void ConvertListValueToSet()
        {
            // arrange
            IValue value = new ListValue
            (
                new IValue[]
                {
                    new StringValue("s"),
                    new StringValue("a"),
                    new StringValue("s")
                }
            );

            // act
            ISet<object> list = ValueConverter.Convert<ISet<object>>(value);

            // assert
            Assert.NotNull(list);
            Assert.Collection(list,
                t => Assert.Equal("s", t),
                t => Assert.Equal("a", t));
        }

        [Fact]
        public void ConvertInputObjectToDictionary()
        {
            // arrange
            IValue value = new InputObjectValue
            (
                new Dictionary<string, IValue>
                {
                    { "a",  new InputObjectValue
                            (
                                new Dictionary<string, IValue>
                                {
                                    { "b",  new ListValue
                                            (
                                                new IValue[]
                                                {
                                                    new StringValue("x"),
                                                    new StringValue("y")
                                                }
                                            )}
                                }
                            )},
                    {"c", new StringValue("z")}
                }
            );

            // act
            IDictionary<string, object> dictionary = ValueConverter
                .Convert<IDictionary<string, object>>(value);

            // assert
            Assert.True(dictionary.ContainsKey("a"));
            Assert.True(dictionary.ContainsKey("c"));
            Assert.Equal("z", dictionary["c"]);

            IList<object> list = (IList<object>)((IDictionary<string, object>)dictionary["a"])["b"];
            Assert.Collection(list,
               t => Assert.Equal("x", t),
               t => Assert.Equal("y", t));
        }

        [Fact]
        public void ConvertInputObjectToObject()
        {
            // arrange
            IValue value = new InputObjectValue
            (
                new Dictionary<string, IValue>
                {
                    { "a",  new InputObjectValue
                            (
                                new Dictionary<string, IValue>
                                {
                                    { "b",  new ListValue
                                            (
                                                new IValue[]
                                                {
                                                    new StringValue("x"),
                                                    new StringValue("y")
                                                }
                                            )}
                                }
                            )},
                    {"c", new StringValue("z")}
                }
            );

            // act
            InputObjectDto obj = ValueConverter
                .Convert<InputObjectDto>(value);

            // assert
            Assert.NotNull(obj);
            Assert.NotNull(obj.A);
            Assert.Equal("z", obj.C);

            Assert.Collection(obj.A.Items,
               t => Assert.Equal("x", t),
               t => Assert.Equal("y", t));
        }

        private class InputObjectDto
        {
            public InputObjectDtoChild A { get; set; }
            public string C { get; set; }
        }

        private class InputObjectDtoChild
        {
            [GraphQLName("b")]
            public IList<string> Items { get; set; }
        }
    }
}