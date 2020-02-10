using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class ParentAttributeTest
    {

        [Fact]
        public void GenericObjectType_ParentInvalidPropertyResolver_ShouldFailGracefully()
        {
            // arrange
            var objectType = new ObjectType<Foo>(
                t => t.Field<FooResolver>(f => f.GetInvalidProperty(default)).Name("desc"));

            //act & assert
            InvalidOperationException ex
                = Assert.Throws<InvalidOperationException>(() =>
            {
                Schema.Create(t => t.RegisterQueryType(objectType));
            });

            ex.Message.MatchSnapshot();
        }

        [Fact]
        public void GenericObjectType_ParentInvalidPropertyTypeResolver_ShouldFailGracefully()
        {
            // arrange
            var objectType = new ObjectType<Foo>(
                t => t.Field<FooResolver>(f => f.GetWrongTypeProperty(default)).Name("desc"));

            //act & assert
            InvalidOperationException ex
                = Assert.Throws<InvalidOperationException>(() =>
                {
                    Schema.Create(t => t.RegisterQueryType(objectType));
                });

            ex.Message.MatchSnapshot();
        }

        [Fact]
        public async Task GenericObjectType_ParentPropertyResolver_BindsPropertyCorrectly()
        {
            // arrange
            var objectType = new ObjectType<Foo>(
                t => t.Field<FooResolver>(f => f.GetParentProperty(default)).Name("desc"));
            var schema = Schema.Create(t => t.RegisterQueryType(objectType));
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ desc }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as ReadOnlyQueryResult;
            Assert.NotNull(queryResult);
            Assert.Empty(queryResult.Errors);
            Assert.Equal("hello", queryResult.Data["desc"]);
        }

        [Fact]
        public async Task GenericObjectType_ParentPropertyNestedResolver_BindsPropertyCorrectly()
        {
            // arrange
            var objectType = new ObjectType<Foo>(
                t => t.Field<FooResolver>(f => f.GetParentPropertyBar(default)).Name("desc"));
            var schema = Schema.Create(t => t.RegisterQueryType(objectType));
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ desc }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as ReadOnlyQueryResult;
            Assert.NotNull(queryResult);
            Assert.Empty(queryResult.Errors);
            Assert.Equal("nested", queryResult.Data["desc"]);
        }

        [Fact]
        public async Task GenericObjectType_ParentPropertyInterfaceResolver_BindsCorrectly()
        {
            // arrange
            var objectType = new ObjectType<Foo>(
                t => t.Field<FooResolver>(f => f.GetParentPropertyIBar(default)).Name("desc"));
            var schema = Schema.Create(t => t.RegisterQueryType(objectType));
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ desc }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as ReadOnlyQueryResult;
            Assert.NotNull(queryResult);
            Assert.Empty(queryResult.Errors);
            Assert.Equal("nested", queryResult.Data["desc"]);
        }

        [Fact]
        public async Task GenericObjectType_ParentResolver_BindsParentCorrectly()
        {
            // arrange
            var objectType = new ObjectType<Foo>(
                t => t.Field<FooResolver>(f => f.GetParent(default)).Name("desc"));
            var schema = Schema.Create(t => t.RegisterQueryType(objectType));
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ desc }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as ReadOnlyQueryResult;
            Assert.NotNull(queryResult);
            Assert.Empty(queryResult.Errors);
            Assert.Equal("hello", queryResult.Data["desc"]);
        }

        [Fact]
        public void ObjectType_ParentPropertyResolver_ShouldFailGracefully()
        {
            // arrange
            var objectType = new ObjectType(t => t.Name("Bar")
                .Field<FooResolver>(f => f.GetParentProperty(default))
                .Name("desc")
                .Type<StringType>());

            //act & assert
            InvalidOperationException ex
                = Assert.Throws<InvalidOperationException>(() =>
                {
                    Schema.Create(t => t.RegisterQueryType(objectType));
                });

            ex.Message.MatchSnapshot();
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
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ desc }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as ReadOnlyQueryResult;
            Assert.NotNull(queryResult);
            Assert.Empty(queryResult.Errors);
            Assert.Equal("hello", queryResult.Data["desc"]);
        }

        [Fact]
        public async Task ExtensionObjectTypeGeneric_ParentProperty_BindsPropertyCorrectly()
        {
            // arrange 
            var schema = Schema.Create(
                t => t.RegisterQueryType<Foo>().RegisterType<FooExtension>());
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ parentProperty }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as ReadOnlyQueryResult;
            Assert.NotNull(queryResult);
            Assert.Empty(queryResult.Errors);
            Assert.Equal("hello", queryResult.Data["parentProperty"]);
        }

        [Fact]
        public void ExtensionObjectType_ParentPropertyResolver_ShouldFailGracefully()
        {
            // arrange 
            var objectType = new ObjectType(t => t.Name("Foo"));

            //act & assert
            InvalidOperationException ex
                = Assert.Throws<InvalidOperationException>(() =>
                {
                    Schema.Create(
                        t => t.RegisterQueryType(objectType).RegisterType<FooExtension>());
                });

            ex.Message.MatchSnapshot();
        }

        [Fact]
        public async Task ExternalResolver_ParentProperty_BindsPropertyCorrectly()
        {
            // arrange  
            var schema = Schema.Create(
                t => t.RegisterQueryType<Foo>().RegisterType<FooWorkingResolvers>());
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ description }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            var queryResult = result as ReadOnlyQueryResult;
            Assert.NotNull(queryResult);
            Assert.Empty(queryResult.Errors);
            Assert.Equal("hellocustom", queryResult.Data["description"]);
        }

        [Fact]
        public void ExternalResolver_ParentProperty_ShouldSkipResolver()
        {
            // arrange  
            // act
            // assert
            SchemaException ex
                = Assert.Throws<SchemaException>(() =>
                {
                    Schema.Create(
                        t => t.RegisterQueryType<Foo>()
                        .RegisterType<FooSkippingResolvers>());
                });

            ex.Message.MatchSnapshot();
        }

        [Fact]
        public void ExternalResolver_ParentProperty_Collision()
        {
            // arrange  
            // act
            // assert
            SchemaException ex
                = Assert.Throws<SchemaException>(() =>
                {
                    Schema.Create(
                        t => t.RegisterQueryType<FooCollision>()
                        .RegisterType<FooCollisionResolver>());
                });

            ex.Message.MatchSnapshot();
        }

        [Fact]
        public async Task ExternalResolver_ParentProperty_NoCollision()
        {
            // arrange  
            var schema = Schema.Create(
                t => t.RegisterQueryType<FooCollision>().RegisterType<FooNoCollisionResolver>());
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ recursive {description} }")
                    .SetInitialValue(new FooCollision())
                    .Create());

            // assert
            var queryResult = result as ReadOnlyQueryResult;
            Assert.NotNull(queryResult);
            Assert.Empty(queryResult.Errors);
            var nested = queryResult.Data["recursive"] as IReadOnlyDictionary<string, object>;
            Assert.Equal("shouldBeInResult", nested["description"]);
        }

        [Fact]
        public void ExternalResolverInclude_ParentProperty_Collision()
        {
            // arrange  
            var objectType = new ObjectType<FooCollision>(
                t => t.Name("FooCollision").Include<FooCollisionResolver>());

            //act & assert
            SchemaException ex
                = Assert.Throws<SchemaException>(() =>
                {
                    Schema.Create(t => t.RegisterQueryType(objectType));
                });

            ex.Message.MatchSnapshot();
        }

        [Fact]
        public async Task ExternalResolverInclude_ParentProperty_NoCollision()
        {
            // arrange  
            var objectType = new ObjectType<FooCollision>(
                t => t.Name("FooCollision").Include<FooNoCollisionResolver>());
            var schema = Schema.Create(
                t => t.RegisterQueryType(objectType));
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ recursive {description} }")
                    .SetInitialValue(new FooCollision())
                    .Create());

            // assert
            var queryResult = result as ReadOnlyQueryResult;
            Assert.NotNull(queryResult);
            Assert.Empty(queryResult.Errors);
            var nested = queryResult.Data["recursive"] as IReadOnlyDictionary<string, object>;
            Assert.Equal("shouldBeInResult", nested["description"]);
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
            public string GetParentProperty([Parent("Description")]string desc) => desc;
            public string GetParentPropertyBar([Parent("Bar")]Bar bar) => bar.Description;
            public string GetParentPropertyIBar([Parent("Bar")]IBar bar) => bar.Description;
            public bool GetWrongTypeProperty([Parent("Description")]bool desc) => desc;
            public string GetInvalidProperty([Parent("DoesNotExists")]string desc) => desc;
        }

        [ExtendObjectType(Name = "Foo")]
        public class FooExtension
        {
            public string GetParentProperty([Parent("Description")]string desc) => desc;
        }

        [GraphQLResolverOf("Foo")]
        public class FooWorkingResolvers
        {
            public string GetDescription([Parent]Foo foo) => foo.Description + "custom";
        }

        [GraphQLResolverOf("Foo")]
        public class FooSkippingResolvers
        {
            public string GetDescription([Parent("Description")]string foo) => foo + "custom";
        }

        [GraphQLResolverOf("FooCollision")]
        public class FooCollisionResolver
        {
            public FooCollision GetRecursive([Parent("Recursive")]FooCollision foo)
            {
                foo.Description = "shouldNotBeInResult";
                return foo;
            }
        }

        [GraphQLResolverOf("FooCollision")]
        public class FooNoCollisionResolver
        {
            public FooCollision GetRecursive([Parent]FooCollision foo)
            {
                foo.Description = "shouldBeInResult";
                return foo;
            }
        }
    }
}
