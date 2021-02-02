using System.Collections.Generic;
using System;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Moq;
using Xunit;
using Snapshooter.Xunit;

namespace HotChocolate.Execution
{
    public class VariableToValueRewriterTests
    {
        [Fact]
        public void Value_Is_Null()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo")
                    .Field("bar").Type<StringType>()))
                .Create();

            var type = schema.GetType<InputObjectType>("Foo");
            var variables = Mock.Of<IVariableValueCollection>();
            var typeConversion = new TypeConversion();

            // act
            Action action = () => VariableToValueRewriter.Rewrite(
                null, type, variables, typeConversion);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Type_Is_Null()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo")
                    .Field("bar").Type<StringType>()))
                .Create();

            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "a",
                    new StringValueNode("abc")));
            var variables = Mock.Of<IVariableValueCollection>();
            var typeConversion = new TypeConversion();

            // act
            Action action = () => VariableToValueRewriter.Rewrite(
                value, null, variables, typeConversion);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Variables_Is_Null()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo")
                    .Field("bar").Type<StringType>()))
                .Create();

            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "a",
                    new StringValueNode("abc")));
            var type = schema.GetType<InputObjectType>("Foo");
            var typeConversion = new TypeConversion();

            // act
            Action action = () => VariableToValueRewriter.Rewrite(
                value, type, null, typeConversion);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void TypeConversion_Is_Null()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo")
                    .Field("bar").Type<StringType>()))
                .Create();

            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "a",
                    new StringValueNode("abc")));
            var type = schema.GetType<InputObjectType>("Foo");
            var variables = Mock.Of<IVariableValueCollection>();

            // act
            Action action = () => VariableToValueRewriter.Rewrite(
                value, type, variables, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Replace_Object_Variable_First_Level()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo")
                    .Field("bar").Type<StringType>()))
                .Create();

            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "bar",
                    new VariableNode("abc")));
            var type = schema.GetType<InputObjectType>("Foo");
            var variables = new VariableCollectionMock("abc", "def");
            var typeConversion = new TypeConversion();

            // act
            IValueNode rewritten = VariableToValueRewriter.Rewrite(
                value, type, variables, typeConversion);

            // assert
            QuerySyntaxSerializer.Serialize(rewritten).MatchSnapshot();
        }

        [Fact]
        public void Replace_Object_Variable_Second_Level()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo")
                    .Field("bar").Type(new NamedTypeNode("Bar"))))
                .AddType(new InputObjectType(d => d
                    .Name("Bar")
                    .Field("baz").Type<StringType>()))
                .Create();

            var innerValue = new ObjectValueNode(
                new ObjectFieldNode(
                    "baz",
                    new VariableNode("abc")));

            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "bar",
                    innerValue));

            var type = schema.GetType<InputObjectType>("Foo");
            var variables = new VariableCollectionMock("abc", "def");
            var typeConversion = new TypeConversion();

            // act
            IValueNode rewritten = VariableToValueRewriter.Rewrite(
                value, type, variables, typeConversion);

            // assert
            QuerySyntaxSerializer.Serialize(rewritten).MatchSnapshot();
        }

        [Fact]
        public void Replace_List_Variable_Second_Level()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo")
                    .Field("bar").Type<ListType<StringType>>()))
                .Create();

            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "bar",
                    new ListValueNode(new VariableNode("abc"))));
            var type = schema.GetType<InputObjectType>("Foo");
            var variables = new VariableCollectionMock("abc", "def");
            var typeConversion = new TypeConversion();

            // act
            IValueNode rewritten = VariableToValueRewriter.Rewrite(
                value, type, variables, typeConversion);

            // assert
            QuerySyntaxSerializer.Serialize(rewritten).MatchSnapshot();
        }

        [Fact]
        public void Replace_Variable_In_Object_List_Object()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo1")
                    .Field("bar")
                    .Type(new ListTypeNode(new NamedTypeNode("Foo2")))))
                .AddType(new InputObjectType(d => d
                    .Name("Foo2")
                    .Field("bar")
                    .Type<ListType<StringType>>()))
                .Create();

            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "bar",
                    new ListValueNode(
                        new ObjectValueNode(
                            new ObjectFieldNode(
                                "bar",
                                new VariableNode("abc"))))));

            var type = schema.GetType<InputObjectType>("Foo1");
            var variables = new VariableCollectionMock("abc", "def");
            var typeConversion = new TypeConversion();

            // act
            IValueNode rewritten = VariableToValueRewriter.Rewrite(
                value, type, variables, typeConversion);

            // assert
            QuerySyntaxSerializer.Serialize(rewritten).MatchSnapshot();
        }

        [Fact]
        public void Cannot_Convert_Variable()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo")
                    .Field("bar").Type<StringType>()))
                .Create();

            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "bar",
                    new VariableNode("abc")));
            var type = schema.GetType<InputObjectType>("Foo");
            var variables = new VariableCollectionMock("abc", new Foo());
            var typeConversion = new TypeConversion();

            // act
            Action action = () => VariableToValueRewriter.Rewrite(
                value, type, variables, typeConversion);

            // assert
            Assert.Throws<QueryException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void Convert_Variable()
        {
            // arrange
            ISchema schema = CreateSchemaBuilder()
                .AddType(new InputObjectType(d => d
                    .Name("Foo")
                    .Field("bar").Type<StringType>()))
                .Create();

            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "bar",
                    new VariableNode("abc")));
            var type = schema.GetType<InputObjectType>("Foo");
            var variables = new VariableCollectionMock("abc", 123);
            var typeConversion = new TypeConversion();

            // act
            IValueNode rewritten = VariableToValueRewriter.Rewrite(
                value, type, variables, typeConversion);

            // assert
            QuerySyntaxSerializer.Serialize(rewritten).MatchSnapshot();
        }


        private ISchemaBuilder CreateSchemaBuilder()
        {
            return SchemaBuilder.New()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar"));
        }

        private class VariableCollectionMock
            : IVariableValueCollection
        {
            private Dictionary<string, object> _values =
                new Dictionary<string, object>();

            public VariableCollectionMock(string name, object value)
            {
                _values[name] = value;
            }

            public T GetVariable<T>(NameString name)
            {
                return (T)_values[name];
            }

            public bool TryGetVariable<T>(NameString name, out T value)
            {
                if (_values.TryGetValue(name, out object v)
                    && v is T casted)
                {
                    value = casted;
                    return true;
                }

                value = default;
                return false;
            }
        }

        private class Foo { }
    }
}
