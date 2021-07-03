# Testing the GraphQL server

There are many ways to test; what we want to have a look at is how we can test parts of the GraphQL schema without writing system tests.

## Add a schema change test

A schema change test will simply create a snapshot of your schema and always fails if the schema changes. This kind of test is often useful when working with pure code-first, where a simple change in C# can create a breaking change in your GraphQL schema.

1. Create a xunit test project.

   ```console
   dotnet new xunit -n GraphQL.Tests
   ```

1. Add the project to our solution.

   ```console
   dotnet sln add GraphQL.Tests
   ```

1. Add a reference to the NuGet package package `Snapshooter.Xunit` version `0.5.7`.

   1. `dotnet add GraphQL.Tests package Snapshooter.Xunit --version 0.5.7`

1. Add a reference to the NuGet package package `Microsoft.EntityFrameworkCore.InMemory` version `5.0.0`.

   1. `dotnet add GraphQL.Tests package Microsoft.EntityFrameworkCore.InMemory --version 5.0.0`

1. Head over to the `GraphQL.Tests.csproj` and change the version of `xunit` to `2.4.1`.

   ```msbuild
   <PackageReference Include="xunit" Version="2.4.1" />
   ```

1. Add reference to the GraphQL server.

   1. `dotnet add GraphQL.Tests reference GraphQL`

1. Rename the file `UnitTest1.cs` to `AttendeeTests.cs` and replace the code with the following:

    ```csharp
    using System;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using ConferencePlanner.GraphQL;
    using ConferencePlanner.GraphQL.Attendees;
    using ConferencePlanner.GraphQL.Data;
    using ConferencePlanner.GraphQL.Sessions;
    using ConferencePlanner.GraphQL.Speakers;
    using ConferencePlanner.GraphQL.Tracks;
    using ConferencePlanner.GraphQL.Types;
    using HotChocolate;
    using HotChocolate.Execution;
    using Snapshooter.Xunit;
    using Xunit;

    namespace GraphQL.Tests
    {
        public class AttendeeTests
        {
            [Fact]
            public async Task Attendee_Schema_Changed()
            {
                // arrange
                // act
                ISchema schema = await new ServiceCollection()
                    .AddPooledDbContextFactory<ApplicationDbContext>(
                        options => options.UseInMemoryDatabase("Data Source=conferences.db"))
                    .AddGraphQL()
                    .AddQueryType(d => d.Name("Query"))
                        .AddTypeExtension<AttendeeQueries>()
                    .AddMutationType(d => d.Name("Mutation"))
                        .AddTypeExtension<AttendeeMutations>()
                    .AddType<AttendeeType>()
                    .AddType<SessionType>()
                    .AddType<SpeakerType>()
                    .AddType<TrackType>()
                    .EnableRelaySupport()
                    .BuildSchemaAsync();
                
                // assert
                schema.Print().MatchSnapshot();
            }
        }
    }
    ```

   The above test takes the service collection and builds from it a schema. We only integrates the part needed that we want to snapshot. On the schema, we are doing a `Print` that will print out the GraphQL SDL representation of the schema on which we do a `MatchSnapshot` that will create in the first run a snapshot file and will compare the SDL in consecutive runs against the snapshot file.

## Add a simple query tests

1. Add the following test to the AttendeeTests.cs:

    ```csharp
    [Fact]
    public async Task RegisterAttendee()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
            .AddPooledDbContextFactory<ApplicationDbContext>(
                options => options.UseInMemoryDatabase("Data Source=conferences.db"))
            .AddGraphQL()
            .AddQueryType(d => d.Name("Query"))
                .AddTypeExtension<AttendeeQueries>()
            .AddMutationType(d => d.Name("Mutation"))
                .AddTypeExtension<AttendeeMutations>()
            .AddType<AttendeeType>()
            .AddType<SessionType>()
            .AddType<SpeakerType>()
            .AddType<TrackType>()
            .EnableRelaySupport()
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync(@"
            mutation RegisterAttendee {
                registerAttendee(
                    input: {
                        emailAddress: ""michael@chillicream.com""
                            firstName: ""michael""
                            lastName: ""staib""
                            userName: ""michael3""
                        })
                {
                    attendee {
                        id
                    }
                }
            }");

        // assert
        result.ToJson().MatchSnapshot();
    }
    ```

   In the above test, we again only take the parts of the schema builder that we are concerned about within our test. Also, we have replaced the services that we do not need at this point.

   To execute against a schema we can call `BuildRequestExecutorAsync` on the service collection and get an `IRequestExecutor` to execute queries against our schema. Finally, we snapshot on the result object, and like in the above test, consecutive tests will be validated against our snapshot file.

[**<< Session #7 - Adding real-time functionality with subscriptions**](7-subscriptions.md)