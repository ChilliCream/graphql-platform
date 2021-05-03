using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Integration.Regressions
{
    public class VariableCoercionIntegrationTests
    {
        [Fact]
        public async Task Nullables_And_NonNullables_Are_Set()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            var user = new ObjectValueNode(
                new ObjectFieldNode("name", "Oliver"),
                new ObjectFieldNode("surname", "Smith"));

            IReadOnlyQueryRequest request =
                QueryRequestBuilder
                    .New()
                    .SetQuery("mutation($user: UserInput!) { addUser(user: $user) }")
                    .SetVariableValue("user", user)
                    .Create();

            await executor.ExecuteAsync(request).MatchSnapshotAsync();
        }

        [Fact]
        public async Task Nullables_Are_Not_Set_NonNullables_Are_Set()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            var user = new ObjectValueNode(
                new ObjectFieldNode("name", "Oliver"));

            IReadOnlyQueryRequest request =
                QueryRequestBuilder
                    .New()
                    .SetQuery("mutation($user: UserInput!) { addUser(user: $user) }")
                    .SetVariableValue("user", user)
                    .Create();

            await executor.ExecuteAsync(request).MatchSnapshotAsync();
        }

        [Fact]
        public async Task Nullables_Are_Set_And_NonNullables_Are_Set_To_Null()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            var user = new ObjectValueNode(
                new ObjectFieldNode("name", NullValueNode.Default),
                new ObjectFieldNode("surname", "Smith"));

            IReadOnlyQueryRequest request =
                QueryRequestBuilder
                    .New()
                    .SetQuery("mutation($user: UserInput!) { addUser(user: $user) }")
                    .SetVariableValue("user", user)
                    .Create();

            await executor.ExecuteAsync(request).MatchSnapshotAsync();
        }

        [Fact]
        public async Task Nullables_Are_Set_And_NonNullables_Not_Are_Set()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            var user = new ObjectValueNode(
                new ObjectFieldNode("surname", "Smith"));

            IReadOnlyQueryRequest request =
                QueryRequestBuilder
                    .New()
                    .SetQuery("mutation($user: UserInput!) { addUser(user: $user) }")
                    .SetVariableValue("user", user)
                    .Create();

            await executor.ExecuteAsync(request).MatchSnapshotAsync();
        }

        [Fact]
        public async Task Empty_Object()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            var user = new ObjectValueNode();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder
                    .New()
                    .SetQuery("mutation($user: UserInput!) { addUser(user: $user) }")
                    .SetVariableValue("user", user)
                    .Create();

            await executor.ExecuteAsync(request).MatchSnapshotAsync();
        }

        [Fact]
        public async Task Variable_Null()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder
                    .New()
                    .SetQuery("mutation($user: UserInput!) { addUser(user: $user) }")
                    .SetVariableValue("user", null)
                    .Create();

            await executor.ExecuteAsync(request).MatchSnapshotAsync();
        }

        [Fact]
        public async Task Variable_Not_Provided()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder
                    .New()
                    .SetQuery("mutation($user: UserInput!) { addUser(user: $user) }")
                    .Create();

            await executor.ExecuteAsync(request).MatchSnapshotAsync();
        }

        [Fact]
        public async Task Invalid_Field_Provided()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            var user = new ObjectValueNode(
                new ObjectFieldNode("name", "Oliver"),
                new ObjectFieldNode("surname", "Smith"),
                new ObjectFieldNode("foo", "bar"));

            IReadOnlyQueryRequest request =
                QueryRequestBuilder
                    .New()
                    .SetQuery("mutation($user: UserInput!) { addUser(user: $user) }")
                    .SetVariableValue("user", user)
                    .Create();

            await executor.ExecuteAsync(request).MatchSnapshotAsync();
        }

        private static async Task<IRequestExecutor> CreateSchemaAsync()
        {
            return await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(t => t.Field("a").Resolve("b"))
                .AddMutationType<UserMutationType>()
                .BuildRequestExecutorAsync();
        }

        public class UserMutation
        {
            public string AddUser(User user)
            {
                return user.Name + " " + user.Surname + " was added!";
            }
        }

        public class UserMutationType : ObjectType<UserMutation>
        {
            protected override void Configure(IObjectTypeDescriptor<UserMutation> descriptor)
            {
                descriptor
                    .Field(um => um.AddUser(default))
                    .Description("Add user to db")
                    .Argument("user", d => d.Type<NonNullType<UserInputType>>()
                        .Description("User input type, required"));
            }
        }

        public class User
        {
            public string Name { get; set; }

            public string Surname { get; set; }
        }

        public class UserInputType : InputObjectType<User>
        {
            protected override void Configure(IInputObjectTypeDescriptor<User> descriptor)
            {
                descriptor
                    .Field(f => f.Name)
                    .Type<NonNullType<StringType>>()
                    .Description("User's name, required");

                descriptor.Field(f => f.Surname)
                    .Description("User's surname");
            }
        }
    }
}
