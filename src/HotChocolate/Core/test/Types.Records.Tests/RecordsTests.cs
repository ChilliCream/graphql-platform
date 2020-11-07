using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types.Relay;
using static HotChocolate.Tests.TestHelper;
using System.Threading.Tasks;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.Execution;

namespace HotChocolate.Types
{
    public class RecordsTests
    {
        [Fact]
        public async Task Records_Clone_Member_Is_Removed()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Records_Default_Value_Is_Taken_From_Ctor()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Relay_Id_Middleware_Is_Correctly_Applied()
        {
            Snapshot.FullName();

            await ExpectValid(
                    @"{ person { id name } }",
                    b => b.AddQueryType<Query>()
                )
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Records_GraphQLNameAttribute()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    x => x.Name("Query")
                        .Field("person")
                        .Resolve(new RenameRecordTest(1, "Michael")))
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Records_GraphQLDescriptionAttribute()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    x => x.Name("Query")
                        .Field("person")
                        .Resolve(new DescriptionRecordTest(1, "Michael")))
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Records_GraphQLDeprecatedAttribute()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    x => x.Name("Query")
                        .Field("person")
                        .Resolve(new DeprecatedRecordTest(1, "Michael")))
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync()
                .MatchSnapshotAsync();
        }

        public class Query
        {
            public Person GetPerson() => new Person(1, "Michael");
        }

        public record Person([ID] int Id, string Name);

        public class Query2
        {
            public DefaultValueTest GetPerson(DefaultValueTest? defaultValueTest) =>
                new DefaultValueTest(1, "Test");
        }

        public record DefaultValueTest([ID] int Id, string Name = "ShouldBeDefaultValue");

        [GraphQLName("Person")]
        public record RenameRecordTest(
            [ID] int Id,
            [GraphQLName("Foo")] string Name = "ShouldBeDefaultValue");

        [GraphQLDescription("Person")]
        public record DescriptionRecordTest(
            [ID] int Id,
            [GraphQLDescription("Foo")] string Name = "ShouldBeDefaultValue");

        public record DeprecatedRecordTest(
            [ID] int Id,
            [GraphQLDeprecated("Foo")] string Name = "ShouldBeDefaultValue");
    }
}
