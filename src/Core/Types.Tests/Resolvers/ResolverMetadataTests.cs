using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class ResolverMetadataTests
    {
        [Fact]
        public void ObjectTypeGeneric_LambdaResolver()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t =>
            {
                t.BindFieldsExplicitly();
                t.Field("foo1").Resolver(x => "Test");
                t.Field(x => x.Description);
            });

            // act
            var schema = Schema.Create(t => t.RegisterQueryType(objectType));

            // assert
            ObjectType<Foo> type = schema.GetType<ObjectType<Foo>>("Foo");
            Assert.False(type.Fields["foo1"].Metadata.IsPure);
            Assert.True(type.Fields["description"].Metadata.IsPure);
            Assert.Single(type.Fields["description"].Metadata.DependsOn);
            Assert.Equal("Description", type.Fields["description"].Metadata.DependsOn[0]);
        }

        [Fact]
        public void ObjectType_LambdaResolver()
        {
            // arrange
            var objectType = new ObjectType(t =>
            {
                t.Name("Foo");
                t.Field("foo1").Resolver(x => "Test");
            });

            // act
            var schema = Schema.Create(t => t.RegisterQueryType(objectType));

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.False(type.Fields["foo1"].Metadata.IsPure);
        }

        [Fact]
        public void ObjectTypeGeneric_ExtensionType()
        {
            // arrange
            var objectType = new ObjectType<Foo>();

            // act
            var schema = Schema.Create(
                t => t.RegisterQueryType(objectType)
                        .RegisterType<FooExtensions>());

            // assert
            ObjectType<Foo> type = schema.GetType<ObjectType<Foo>>("Foo");
            Assert.True(type.Fields["method1"].Metadata.IsPure);

            Assert.True(type.Fields["method3"].Metadata.IsPure);

            Assert.False(type.Fields["method4"].Metadata.IsPure);
            Assert.Empty(type.Fields["method4"].Metadata.DependsOn);

            Assert.False(type.Fields["method5"].Metadata.IsPure);
            Assert.Empty(type.Fields["method5"].Metadata.DependsOn);
        }

        [Fact]
        public void ObjectType_Inferred()
        {
            // arrange
            var objectType = new ObjectType<FooInferred>();

            // act
            var schema = Schema.Create(
                t => t.RegisterQueryType(objectType));

            // assert
            ObjectType<FooInferred> type =
                schema.GetType<ObjectType<FooInferred>>("FooInferred");
            Assert.False(type.Fields["method1"].Metadata.IsPure);
            Assert.True(type.Fields["description"].Metadata.IsPure);

            Assert.False(type.Fields["method3"].Metadata.IsPure);

            Assert.False(type.Fields["method4"].Metadata.IsPure);
            Assert.Empty(type.Fields["method4"].Metadata.DependsOn);

            Assert.False(type.Fields["method5"].Metadata.IsPure);
            Assert.Empty(type.Fields["method5"].Metadata.DependsOn);
        }

        [Fact]
        public void ObjectType_ExtensionType()
        {
            // arrange
            var objectType = new ObjectType(x => x.Name("Foo"));

            // act
            var schema = Schema.Create(
                t => t.RegisterQueryType(objectType)
                        .RegisterType<FooExtensionsNonGeneric>());

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["method1"].Metadata.IsPure);

            Assert.True(type.Fields["method3"].Metadata.IsPure);

            Assert.False(type.Fields["method4"].Metadata.IsPure);
            Assert.Empty(type.Fields["method4"].Metadata.DependsOn);

            Assert.False(type.Fields["method5"].Metadata.IsPure);
            Assert.Empty(type.Fields["method5"].Metadata.DependsOn);
        }

        [Fact]
        public void ObjectTypeGeneric_Include()
        {
            // arrange
            var objectType = new ObjectType<Foo>(
                x => x.Include<FooResolver>());

            // act
            var schema = Schema.Create(
                t => t.RegisterQueryType(objectType));

            // assert
            ObjectType<Foo> type = schema.GetType<ObjectType<Foo>>("Foo");
            Assert.True(type.Fields["method1"].Metadata.IsPure);

            Assert.True(type.Fields["method3"].Metadata.IsPure);

            Assert.False(type.Fields["method4"].Metadata.IsPure);
            Assert.Empty(type.Fields["method4"].Metadata.DependsOn);

            Assert.False(type.Fields["method5"].Metadata.IsPure);
            Assert.Empty(type.Fields["method5"].Metadata.DependsOn);
        }

        [Fact]
        public void ObjectType_Include()
        {
            // arrange
            var objectType = new ObjectType(
                x => x.Name("Foo").Include<FooResolver>());

            // act
            var schema = Schema.Create(
                t => t.RegisterQueryType(objectType));

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["method1"].Metadata.IsPure);

            Assert.True(type.Fields["method3"].Metadata.IsPure);

            Assert.False(type.Fields["method4"].Metadata.IsPure);
            Assert.Empty(type.Fields["method4"].Metadata.DependsOn);

            Assert.False(type.Fields.ContainsField("method5"));
        }

        [Fact]
        public void ObjectTypeGeneric_IncludeDirectly()
        {
            // arrange
            var objectType = new ObjectType<Foo>(
                t =>
                {
                    t.Field<FooExtensions>(x => x.Method1());
                    t.Field<FooExtensions>(x => x.Method3(default));
                    t.Field<FooExtensions>(x => x.Method4(default));
                    t.Field<FooExtensions>(x => x.Method5(default));
                });

            // act
            var schema = Schema.Create(
                t => t.RegisterQueryType(objectType));

            // assert
            ObjectType<Foo> type = schema.GetType<ObjectType<Foo>>("Foo");
            Assert.True(type.Fields["method1"].Metadata.IsPure);

            Assert.True(type.Fields["method3"].Metadata.IsPure);

            Assert.False(type.Fields["method4"].Metadata.IsPure);
            Assert.Empty(type.Fields["method4"].Metadata.DependsOn);

            Assert.False(type.Fields["method5"].Metadata.IsPure);
            Assert.Empty(type.Fields["method5"].Metadata.DependsOn);
        }

        [Fact]
        public void ObjectType_IncludeDirectly()
        {
            // arrange
            var objectType = new ObjectType(
                t =>
                {
                    t.Name("Foo");
                    t.Field<FooExtensions>(x => x.Method1());
                    t.Field<FooExtensions>(x => x.Method3(default));
                    t.Field<FooExtensions>(x => x.Method4(default));
                    t.Field<FooExtensions>(x => x.Method5(default));
                });

            // act
            var schema = Schema.Create(
                t => t.RegisterQueryType(objectType));

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["method1"].Metadata.IsPure);

            Assert.True(type.Fields["method3"].Metadata.IsPure);

            Assert.False(type.Fields["method4"].Metadata.IsPure);
            Assert.Empty(type.Fields["method4"].Metadata.DependsOn);

            Assert.False(type.Fields["method5"].Metadata.IsPure);
            Assert.Empty(type.Fields["method5"].Metadata.DependsOn);
        }

        [Fact]
        public void ObjectTypeGeneric_GraphQLResolverOf()
        {
            // arrange
            var objectType = new ObjectType<Foo>(x =>
            {
                x.Field("method1").Type<StringType>();
                x.Field("method3").Type<StringType>();
                x.Field("method4").Type<StringType>();
                x.Field("method5").Type<StringType>();
            });

            // act
            var schema = Schema.Create(
                t => t.RegisterQueryType(objectType)
                        .RegisterType<FooResolver>());

            // assert
            ObjectType<Foo> type = schema.GetType<ObjectType<Foo>>("Foo");
            Assert.True(type.Fields["method1"].Metadata.IsPure);

            Assert.True(type.Fields["method3"].Metadata.IsPure);

            Assert.False(type.Fields["method4"].Metadata.IsPure);
            Assert.Empty(type.Fields["method4"].Metadata.DependsOn);

            Assert.False(type.Fields["method5"].Metadata.IsPure);
            Assert.Empty(type.Fields["method5"].Metadata.DependsOn);
        }

        [GraphQLResolverOf("Foo")]
        private class FooResolver
        {
            public string Method1()
            {
                return "";
            }

            public string Method3(string arg)
            {
                return arg;
            }

            public string Method4(IResolverContext context)
            {
                return "asdasd";
            }

            public string Method5([Parent]Foo foo)
            {
                return foo.Description;
            }
        }

        [ExtendObjectType(Name = "Foo")]
        private class FooExtensionsNonGeneric
        {
            public string Method1()
            {
                return "";
            }

            public string Method3(string arg)
            {
                return arg;
            }

            public string Method4(IResolverContext context)
            {
                return "asdasd";
            }

            public string Method5([Parent]Foo foo)
            {
                return foo.Description;
            }
        }

        [ExtendObjectType(Name = "Foo")]
        private class FooExtensions
        {
            public string Method1()
            {
                return "";
            }

            public string Method3(string arg)
            {
                return arg;
            }

            public string Method4(IResolverContext context)
            {
                return "asdasd";
            }

            public string Method5([Parent]Foo foo)
            {
                return foo.Description;
            }
        }

        private class Foo
        {
            public string Description { get; } = "hello";
        }

        private class FooInferred
        {
            public string Description { get; } = "hello";

            public string Method1()
            {
                return "";
            }

            public string Method3(string arg)
            {
                return arg;
            }

            public string Method4(IResolverContext context)
            {
                return "asdasd";
            }

            public string Method5([Parent]Foo foo)
            {
                return foo.Description;
            }
        }
    }
}
