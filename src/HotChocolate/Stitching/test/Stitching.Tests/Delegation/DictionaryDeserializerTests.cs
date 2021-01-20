using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Stitching.Delegation
{
    public class DictionaryDeserializerTests
    {
        [Fact]
        public async Task Deserialize_NullValueNode()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType person = schema.GetType<ObjectType>("Person");

            // act
            object value = DictionaryDeserializer.DeserializeResult(person, NullValueNode.Default);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public async Task Deserialize_Null()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType person = schema.GetType<ObjectType>("Person");

            // act
            object value = DictionaryDeserializer.DeserializeResult(person, null);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public async Task Deserialize_Dictionary()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType person = schema.GetType<ObjectType>("Person");

            var dict = new Dictionary<string, object>();

            // act
            object value = DictionaryDeserializer.DeserializeResult(person, dict);

            // assert
            Assert.Same(dict, value);
        }

        [Fact]
        public async Task Deserialize_String()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType stringType = schema.GetType<StringType>("String");

            // act
            object value = DictionaryDeserializer.DeserializeResult(stringType, "abc");

            // assert
            Assert.Equal("abc", value);
        }

        [Fact]
        public async Task Deserialize_StringValueNode()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType stringType = schema.GetType<StringType>("String");

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                stringType,
                new StringValueNode("abc"));

            // assert
            Assert.Equal("abc", value);
        }

        [Fact]
        public async Task Deserialize_StringList_StringList()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType stringListType = new ListType(schema.GetType<StringType>("String"));

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                stringListType,
                new List<string> { "abc" });

            // assert
            Assert.Collection(
                ((IEnumerable<string>)value)!,
                v => Assert.Equal("abc", v));
        }

        [Fact]
        public async Task Deserialize_StringList_ListOfStringValueNode()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType stringListType = new ListType(schema.GetType<StringType>("String"));

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                stringListType,
                new List<object> { new StringValueNode("abc") });

            // assert
            Assert.Collection(
                ((IEnumerable<string>)value)!,
                v => Assert.Equal("abc", v));
        }

        [Fact]
        public async Task Deserialize_ListValueNode_Enum()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<Foo>())
                            .Type<ListType<EnumType<Foo>>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new ListValueNode(new EnumValueNode(Foo.Bar), new EnumValueNode(Foo.Baz)));

            // assert
            Assert.Collection(
                Assert.IsType<List<object>>(value)!,
                x => Assert.Equal(Foo.Bar, x),
                x => Assert.Equal(Foo.Baz, x));
        }

        [Fact]
        public async Task Deserialize_StringValueNode_Enum()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(default(Foo))
                            .Type<EnumType<Foo>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new StringValueNode("BAZ"));

            // assert
            Assert.Equal(Foo.Baz, value);
        }

        [Fact]
        public async Task Deserialize_String_Enum()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(default(Foo))
                            .Type<EnumType<Foo>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type, "BAZ");

            // assert
            Assert.Equal(Foo.Baz, value);
        }

        [Fact]
        public async Task Deserialize_EnumValueNode_Enum()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(default(Foo))
                            .Type<EnumType<Foo>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new EnumValueNode(Foo.Baz));

            // assert
            Assert.Equal(Foo.Baz, value);
        }

        [Fact]
        public async Task Deserialize_ListEnum()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<Foo>())
                            .Type<ListType<EnumType<Foo>>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new List<object> { new EnumValueNode(Foo.Bar), new EnumValueNode(Foo.Baz) });

            // assert
            Assert.Collection(
                Assert.IsType<List<Foo?>>(value)!,
                x => Assert.Equal(Foo.Bar, x),
                x => Assert.Equal(Foo.Baz, x));
        }

        [Fact]
        public async Task Deserialize_ListNestedEnum()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<Foo>())
                            .Type<ListType<ListType<EnumType<Foo>>>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);


            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new List<object>
                {
                    new List<object> { new EnumValueNode(Foo.Bar), new EnumValueNode(Foo.Baz) }
                });

            // assert
            Assert.Collection(
                Assert.IsType<List<Foo?>>(Assert.IsType<List<List<Foo?>>>(value)!.First())!,
                x => Assert.Equal(Foo.Bar, x),
                x => Assert.Equal(Foo.Baz, x));
        }

        [Fact]
        public async Task Deserialize_NonNull_ListEnum()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<Foo>())
                            .Type<ListType<NonNullType<EnumType<Foo>>>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new List<object> { new EnumValueNode(Foo.Bar), new EnumValueNode(Foo.Baz) });

            // assert
            Assert.Collection(
                Assert.IsType<List<Foo>>(value)!,
                x => Assert.Equal(Foo.Bar, x),
                x => Assert.Equal(Foo.Baz, x));
        }

        [Fact]
        public async Task Deserialize_NonNull_ListNestedEnum()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<Foo>())
                            .Type<ListType<ListType<NonNullType<EnumType<Foo>>>>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);


            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new List<object>
                {
                    new List<object> { new EnumValueNode(Foo.Bar), new EnumValueNode(Foo.Baz) }
                });

            // assert
            Assert.Collection(
                Assert.IsType<List<Foo>>(Assert.IsType<List<List<Foo>>>(value)!.First())!,
                x => Assert.Equal(Foo.Bar, x),
                x => Assert.Equal(Foo.Baz, x));
        }


        [Fact]
        public async Task Deserialize_ListValueNode_Int()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<int>())
                            .Type<ListType<IntType>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new ListValueNode(new IntValueNode(1), new IntValueNode(2)));

            // assert
            Assert.Collection(
                Assert.IsType<List<object>>(value)!,
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x));
        }

        [Fact]
        public async Task Deserialize_IntValueNode_Int()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(1)
                            .Type<IntType>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new IntValueNode(2));

            // assert
            Assert.Equal(2, value);
        }

        [Fact]
        public async Task Deserialize_Int_Int()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(1)
                            .Type<IntType>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type, 2);

            // assert
            Assert.Equal(2, value);
        }

        [Fact]
        public async Task Deserialize_ListInt()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<int>())
                            .Type<ListType<IntType>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new List<object> { new IntValueNode(1), new IntValueNode(2) });

            // assert
            Assert.Collection(
                Assert.IsType<List<int?>>(value)!,
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x));
        }

        [Fact]
        public async Task Deserialize_ListNestedInt()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<int>())
                            .Type<ListType<ListType<IntType>>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);


            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new List<object> { new List<object> { new IntValueNode(1), new IntValueNode(2) } });

            // assert
            Assert.Collection(
                Assert.IsType<List<int?>>(Assert.IsType<List<List<int?>>>(value)!.First())!,
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x));
        }

        [Fact]
        public async Task Deserialize_NonNull_ListInt()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<int>())
                            .Type<ListType<NonNullType<IntType>>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new List<object> { new IntValueNode(1), new IntValueNode(2) });

            // assert
            Assert.Collection(
                Assert.IsType<List<int>>(value)!,
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x));
        }

        [Fact]
        public async Task Deserialize_NonNull_ListNestedInt()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType(x =>
                        x.Name("Query")
                            .Field("Foo")
                            .Resolver(Array.Empty<int>())
                            .Type<ListType<ListType<NonNullType<IntType>>>>())
                    .BuildSchemaAsync();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            queryType.Fields.TryGetField("Foo", out ObjectField fooField);


            // act
            object value = DictionaryDeserializer.DeserializeResult(
                fooField!.Type,
                new List<object> { new List<object> { new IntValueNode(1), new IntValueNode(2) } });

            // assert
            Assert.Collection(
                Assert.IsType<List<int>>(Assert.IsType<List<List<int>>>(value)!.First())!,
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x));
        }

        public class Query
        {
            public Person GetPerson() => new Person();
        }

        public class Person
        {
            public string Name { get; } = "Jon Doe";
        }

        public enum Foo
        {
            Bar,
            Baz
        }
    }
}
