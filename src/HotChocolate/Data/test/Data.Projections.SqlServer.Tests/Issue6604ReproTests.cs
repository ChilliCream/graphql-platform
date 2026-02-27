using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Projections;

public class Issue6604ReproTests
{
    private readonly SchemaCache _cache = new();

    [Fact]
    public async Task Projection_On_NonNullable_Complex_Type_Should_Not_Fail()
    {
        var users =
            new[]
            {
                new User
                {
                    Id = 1,
                    Username = "user-1",
                    EmailAddress = "user-1@example.com",
                    AccountStatus = new UserAccountStatus
                    {
                        IsRegistrationCompleted = true
                    }
                }
            };

        var executor = _cache.CreateSchema(
            users,
            onModelCreating: modelBuilder =>
                modelBuilder.Entity<User>().ComplexProperty(x => x.AccountStatus));

        var result = await executor.ExecuteAsync(
            """
            {
              root {
                id
                username
                accountStatus {
                  isRegistrationCompleted
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
    }

    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = default!;

        public string EmailAddress { get; set; } = default!;

        public UserAccountStatus AccountStatus { get; set; } = default!;
    }

    public class UserAccountStatus
    {
        public bool IsRegistrationCompleted { get; set; }
    }
}
