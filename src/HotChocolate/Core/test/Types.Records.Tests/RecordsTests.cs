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

            await ExpectValid
            (
                @"{ person { id name } }",
                b => b.AddQueryType<Query>()
            )
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
    }
}
