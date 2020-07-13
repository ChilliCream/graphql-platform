using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class ParentAttributeTests
    {
        [Fact]
        public async Task GenericObjectType_ParentResolver_BindsParentCorrectly()
        {
            // arrange
            var objectType = new ObjectType<Foo>(
                t => t.Field<FooResolver>(f => f.GetParent(default)).Name("desc"));
            var schema = Schema.Create(t => t.RegisterQueryType(objectType));
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ desc }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as QueryResult;
            Assert.NotNull(queryResult);
            Assert.Null(queryResult.Errors);
            Assert.Equal("hello", queryResult.Data["desc"]);
        }

        [Fact]
        public async Task ObjectType_ParentResolver_BindsParentCorrectly()
        {
            // arrange
            var objectType = new ObjectType(t => t.Name("Bar")
                .Field<FooResolver>(f => f.GetParent(default))
                .Name("desc")
                .Type<StringType>());
            var schema = Schema.Create(t => t.RegisterQueryType(objectType));
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ desc }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as QueryResult;
            Assert.NotNull(queryResult);
            Assert.Null(queryResult.Errors);
            Assert.Equal("hello", queryResult.Data["desc"]);
        }

        [Fact]
        public async Task ExternalResolver_ParentProperty_BindsPropertyCorrectly()
        {
            // arrange  
            var schema = Schema.Create(
                t => t.RegisterQueryType<Foo>().RegisterType<FooWorkingResolvers>());
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ description }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as QueryResult;
            Assert.NotNull(queryResult);
            Assert.Null(queryResult.Errors);
            Assert.Equal("hellocustom", queryResult.Data["description"]);
        }

        public class Foo
        {
            public Bar Bar { get; } = new Bar();
            public string Description { get; } = "hello";
        }

        public class FooCollision
        {
            public static FooCollision _val = new FooCollision();
            public FooCollision Recursive { get; } = _val;
            public string Description { get; set; } = "hello";
        }

        public class Bar : IBar
        {
            public string Description { get; } = "nested";
        }

        public interface IBar
        {
            string Description { get; }
        }

        public class FooResolver
        {
            public string GetParent([Parent]Foo foo) => foo.Description;
            public string GetPure() => "foo";
        }

        [ExtendObjectType(Name = "Foo")]
        public class FooExtension
        {
            public string GetPure() => "foo";
            public string GetParent([Parent]Foo foo) => foo.Description;
        }

        [GraphQLResolverOf("Foo")]
        public class FooWorkingResolvers
        {
            public string GetDescription([Parent]Foo foo) => foo.Description + "custom";
            public string GetPure() => "foo";
        }
    }
}
