using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IOPath = System.IO.Path;

namespace HotChocolate.Data;

public class InterfaceFieldProjectionTests : IDisposable
{
    private readonly string _fileName = IOPath.Combine(
        IOPath.GetTempPath(),
        $"interface-field-projection-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task Projection_Should_ProjectInterfaceBoundFields_When_BindingIsExplicit()
    {
        // arrange
        // id/firstName/lastName exist on Employee only through the PersonType interface
        // binding; with explicit binding the object type declares just email itself.
        await SeedAsync();

        var executor = await new ServiceCollection()
            .AddDbContext<CompanyDbContext>(
                b => b.UseSqlite("Data Source=" + _fileName))
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddType<PersonType>()
            .AddType<EmployeeType>()
            .AddProjections()
            .ModifyOptions(o => o.DefaultBindingBehavior = BindingBehavior.Explicit)
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              employees {
                id
                firstName
                lastName
                email
              }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "employees": [
                  {
                    "id": 1,
                    "firstName": "Jane",
                    "lastName": "Doe",
                    "email": "jane@acme.com"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task QueryContext_Should_ProjectInterfaceBoundFields_When_BindingIsExplicit()
    {
        // arrange
        // Same scenario, but projected through the QueryContext selector pipeline.
        await SeedAsync();

        var executor = await new ServiceCollection()
            .AddDbContext<CompanyDbContext>(
                b => b.UseSqlite("Data Source=" + _fileName))
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<QueryContextQueryType>()
            .AddType<PersonType>()
            .AddType<EmployeeType>()
            .ModifyOptions(o => o.DefaultBindingBehavior = BindingBehavior.Explicit)
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              employees {
                id
                firstName
                lastName
                email
              }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "employees": [
                  {
                    "id": 1,
                    "firstName": "Jane",
                    "lastName": "Doe",
                    "email": "jane@acme.com"
                  }
                ]
              }
            }
            """);
    }

    private async Task SeedAsync()
    {
        var options = new DbContextOptionsBuilder<CompanyDbContext>()
            .UseSqlite("Data Source=" + _fileName)
            .Options;

        await using var context = new CompanyDbContext(options);
        await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

        context.Employees.Add(
            new Employee
            {
                Id = 1,
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@acme.com"
            });

        await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_fileName))
        {
            File.Delete(_fileName);
        }
    }

    public interface IPerson
    {
        int Id { get; set; }

        string FirstName { get; set; }

        string LastName { get; set; }
    }

    public class Employee : IPerson
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    public class CompanyDbContext(DbContextOptions<CompanyDbContext> options) : DbContext(options)
    {
        public DbSet<Employee> Employees => Set<Employee>();
    }

    public class PersonType : InterfaceType<IPerson>
    {
        protected override void Configure(IInterfaceTypeDescriptor<IPerson> descriptor)
        {
            descriptor.Name("Person");
            descriptor.Field(x => x.Id);
            descriptor.Field(x => x.FirstName);
            descriptor.Field(x => x.LastName);
        }
    }

    public class EmployeeType : ObjectType<Employee>
    {
        protected override void Configure(IObjectTypeDescriptor<Employee> descriptor)
        {
            descriptor.Implements<PersonType>();
            descriptor.Field(x => x.Email);
        }
    }

    public class Query
    {
        public IQueryable<Employee> GetEmployees(CompanyDbContext context)
            => context.Employees;
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor
                .Field(x => x.GetEmployees(null!))
                .UseProjection();
        }
    }

    public class QueryContextQuery
    {
        public IQueryable<Employee> GetEmployees(
            CompanyDbContext context,
            QueryContext<Employee> query)
            => context.Employees.With(query);
    }

    public class QueryContextQueryType : ObjectType<QueryContextQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<QueryContextQuery> descriptor)
        {
            descriptor.Field(x => x.GetEmployees(null!, null!));
        }
    }
}
