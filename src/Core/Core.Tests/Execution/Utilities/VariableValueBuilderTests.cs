using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class VariableValueBuilderTests
    {
        [Fact]
        public void QueryWithNonNullVariableAndDefaultWhereValueWasProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String! = \"foo\") { a }");
            var variableValues = new Dictionary<string, object>
            {
                {
                    "test",
                    new StringValueNode(null, "123456", false)
                }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal("123456",
                coercedVariableValues.GetVariable<string>("test"));
        }

        [Fact]
        public void Coerce_Variable_Value_Int_To_String()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String! = \"foo\") { a }");
            var variableValues = new Dictionary<string, object>
            {
                {
                    "test",
                    123
                }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void Coerce_Variable_Value_Float_To_Int()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: Int!) { a }");
            var variableValues = new Dictionary<string, object>
            {
                {
                    "test",
                    123.123
                }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action).Errors.MatchSnapshot();
        }

         [Fact]
        public void Coerce_Variable_Value_Int_To_Float()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: Float!) { a }");
            var variableValues = new Dictionary<string, object>
            {
                {
                    "test",
                    123
                }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal((float)123,
                coercedVariableValues.GetVariable<float>("test"));
        }

        [Fact]
        public void StringVariableIsObject()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String) { a }");
            var variableValues = new Dictionary<string, object>
            {
                {
                    "test",
                    new Dictionary<string, object>()
                }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action).Errors.MatchSnapshot();
        }

        [Fact]
        public void QueryWithNonNullVariableAndDefaultWhereValueWasNotProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String! = \"foo\") { a }");
            var variableValues = new Dictionary<string, object>
            {
                { "test", NullValueNode.Default }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action);
        }

        [Fact]
        public void QueryWithNonNullVariableAndDefaultWhereValueIsNull()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String! = \"foo\") { a }");
            var variableValues = new Dictionary<string, object>();

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal("foo",
                coercedVariableValues.GetVariable<string>("test"));
        }

        [Fact]
        public void QueryWithNullableVarAndNoDefaultWhereNoValueWasProvided()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: String) { a }");
            var variableValues = new Dictionary<string, object>
            {
                { "test", NullValueNode.Default }
            };

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Null(coercedVariableValues.GetVariable<string>("test"));
        }

        [Fact]
        public void CoerceEnumFromEnum()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarEnum!) { a }");

            var variableValues = new Dictionary<string, object>
            {
                { "test", BarEnum.A }
            };

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal(BarEnum.A,
                coercedVariableValues.GetVariable<BarEnum>("test"));
        }

        [Fact]
        public void CoerceEnumFromStringValueNode()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarEnum!) { a }");

            var variableValues = new Dictionary<string, object>
            {
                { "test", "A" }
            };

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal(BarEnum.A,
                coercedVariableValues.GetVariable<BarEnum>("test"));
        }

        [Fact]
        public void CoerceEnumFromEnumValueNode()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarEnum!) { a }");

            var variableValues = new Dictionary<string, object>
            {
                { "test", new EnumValueNode("A") }
            };

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Assert.Equal(BarEnum.A,
                coercedVariableValues.GetVariable<BarEnum>("test"));
        }

        [Fact]
        public void CoerceInputObjectWithEnumInDictionaryGraph()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarInput!) { a }");

            var fooInput = new Dictionary<string, object>
            {
                { "b", "B" }
            };

            var barInput = new Dictionary<string, object>
            {
                { "f", fooInput }
            };

            var variableValues = new Dictionary<string, object>
            {
                { "test", barInput }
            };

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Bar bar = coercedVariableValues.GetVariable<Bar>("test");
            Assert.NotNull(bar.F);
            Assert.Equal(BarEnum.B, bar.F.B);
        }

        [Fact]
        public void CoerceInputObjectWithEnumAsEnumValueNode()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarInput!) { a }");

            var fooInput = new ObjectValueNode(
                new ObjectFieldNode("b",
                    new EnumValueNode("B")));

            var barInput = new ObjectValueNode(
                new ObjectFieldNode("f", fooInput));

            var variableValues = new Dictionary<string, object>
            {
                { "test", barInput }
            };

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Bar bar = coercedVariableValues.GetVariable<Bar>("test");
            Assert.NotNull(bar.F);
            Assert.Equal(BarEnum.B, bar.F.B);
        }

        [InlineData(int.MaxValue)]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        [Theory]
        public void CreateValues_IntValue_Int(int value)
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: Int) { a }");

            var variableValues = new Dictionary<string, object>
            {
                { "test", value }
            };

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            var result = coercedVariableValues.GetVariable<int>("test");
            Assert.Equal(value, result);
        }

        [Fact]
        public void CreateValues_ObjectAsDictionary_Object()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BarInput!) { a }");

            var fooInput = new Dictionary<string, object>();
            fooInput["b"] = "B";

            var barInput = new Dictionary<string, object>();
            barInput["f"] = fooInput;

            var variableValues = new Dictionary<string, object>();
            variableValues.Add("test", barInput);

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            Bar bar = coercedVariableValues.GetVariable<Bar>("test");
            Assert.NotNull(bar.F);
            Assert.Equal(BarEnum.B, bar.F.B);
        }

        [Fact]
        public void CreateValues_ListOfObject_ListOfString()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: [String]) { a }");

            var variableValues = new Dictionary<string, object>
            {
                { "test", new List<object> { "a", "b" } }
            };

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            var list = coercedVariableValues.GetVariable<string[]>("test");
            Assert.Collection(list,
                t => Assert.Equal("a", t),
                t => Assert.Equal("b", t));
        }

        [Fact]
        public void CreateValues_SerializedDecimal_Decimal()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: Decimal) { a }");
            var input = 1.000000E-004;

            var variableValues = new Dictionary<string, object>();
            variableValues.Add("test", input);

            var resolver = new VariableValueBuilder(schema, operation);

            // act
            VariableValueCollection coercedVariableValues =
                resolver.CreateValues(variableValues);

            // assert
            var result = coercedVariableValues.GetVariable<decimal>("test");
            Assert.Equal(0.0001m, result);
        }

        [Fact]
        public void Variable_InputObjectAsClrType_NonNullFieldNull()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BazInput) { a }");
            var variableValues = new Dictionary<string, object>
            {
                { "test", new Baz { Bar = "bar" } }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action);
        }

        [Fact]
        public void Variable_InputObjectAsDict_NonNullFieldNull()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BazInput) { a }");
            var variableValues = new Dictionary<string, object>
            {
                { "test",  new Dictionary<string, object>
                    {
                        { "bar", "bar" }
                    }
                }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action);
        }

        [Fact]
        public void Variable_InputObjectAsClrType_NonNullListItemNull()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BazInput) { a }");
            var variableValues = new Dictionary<string, object>
            {
                { "test", new Baz
                    {
                        Foo = "foo",
                        Quox = new string[] { null }
                    }
                }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action);
        }

        [Fact]
        public void Variable_InputObjectAsDict_NonNullListItemNull()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: BazInput) { a }");
            var variableValues = new Dictionary<string, object>
            {
                { "test",  new Dictionary<string, object>
                    {
                        { "foo", "foo" },
                        { "quox", new List<object>
                            {
                                null
                            }
                        }
                    }
                }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action);
        }

        [Fact]
        public void Variable_List_NonNullListItemNull()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: [String!]) { a }");
            var variableValues = new Dictionary<string, object>
            {
                { "test",  new List<string> { null }

                }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            Action action = () => resolver.CreateValues(variableValues);

            // assert
            Assert.Throws<QueryException>(action);
        }

        [Fact]
        public void Variable_List_NonNullListItemHasValue()
        {
            // arrange
            Schema schema = CreateSchema();
            OperationDefinitionNode operation = CreateQuery(
                "query test($test: [String!]) { a }");
            var variableValues = new Dictionary<string, object>
            {
                { "test",  new List<string> { "abc" } }
            };

            // act
            var resolver = new VariableValueBuilder(schema, operation);
            VariableValueCollection variables =
                resolver.CreateValues(variableValues);

            // assert
            variables.GetVariable<List<string>>("test").MatchSnapshot();
        }

        [Fact]
        public async Task EnsureThatDateTimeIsCoercedTheSameInAllCases()
        {
            // arrange
            IQueryExecutor executor = Schema.Create(
                "type Query { a(a: DateTime) : DateTime }",
                c =>
                {
                    c.RegisterExtendedScalarTypes();
                    c.Use(next => context =>
                    {
                        context.Result = context.Argument<DateTimeOffset>("a")
                            .ToUniversalTime();
                        return Task.CompletedTask;
                    });
                }).MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(@"
                        query a($d: DateTime!) {
                            a: a(a: ""2018-01-01T01:00:00.000Z"")
                            b: a(a: $d)
                        }")
                    .AddVariableValue("d", "2018-01-01T01:00:00.000Z")
                    .Create());

            // assert
            result.MatchSnapshot();
        }

        private Schema CreateSchema()
        {
            return Schema.Create(
                "type Query { foo: Foo } " +
                "type Foo { a: String b(a: BarInput): String } ",
                c =>
                {
                    c.Use(next => context => Task.CompletedTask);
                    c.RegisterType<BarType>();
                    c.RegisterType<FooType>();
                    c.RegisterType<BazType>();
                    c.RegisterType<BarEnumType>();
                    c.RegisterType<FloatType>();
                    c.RegisterExtendedScalarTypes();
                });
        }

        private OperationDefinitionNode CreateQuery(string query)
        {
            var parser = new Parser();
            return parser.Parse(query)
                .Definitions.OfType<OperationDefinitionNode>().First();
        }

        public class BazType
            : InputObjectType<Baz>
        {
            protected override void Configure(
                IInputObjectTypeDescriptor<Baz> descriptor)
            {
                descriptor.Field(t => t.Foo)
                    .Type<NonNullType<StringType>>();
                descriptor.Field(t => t.Quox)
                    .Type<ListType<NonNullType<StringType>>>();
            }
        }

        public class BarType
            : InputObjectType<Bar>
        {
        }

        public class FooType
            : InputObjectType<Foo>
        {
        }

        public class BarEnumType
            : EnumType<BarEnum>
        {
        }

        public class Bar
        {
            public Foo F { get; set; }
        }

        public class Foo
        {
            public BarEnum B { get; set; }
        }

        public enum BarEnum
        {
            A,
            B
        }

        public class Baz
        {
            public string Foo { get; set; }
            public string Bar { get; set; }
            public string[] Quox { get; set; }
        }
    }
}
