using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class InterfaceFieldProjectionTests
{
    [Fact]
    public async Task Projection_Should_ProjectInterfaceBoundFields_When_BindingIsExplicit()
    {
        // arrange
        // id/firstName/lastName exist on Employee only through the PersonType interface
        // binding; with explicit binding the object type declares just email itself.
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddType<PersonType>()
            .AddType<EmployeeType>()
            .AddProjections()
            .ModifyOptions(o => o.DefaultBindingBehavior = BindingBehavior.Explicit)
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

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
            """);

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
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<QueryContextQueryType>()
            .AddType<PersonType>()
            .AddType<EmployeeType>()
            .ModifyOptions(o => o.DefaultBindingBehavior = BindingBehavior.Explicit)
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

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
            """);

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
        public IQueryable<Employee> GetEmployees()
            => s_employees.AsQueryable();
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor
                .Field(x => x.GetEmployees())
                .UseProjection();
        }
    }

    public class QueryContextQuery
    {
        public IQueryable<Employee> GetEmployees(QueryContext<Employee> query)
            => s_employees.AsQueryable().With(query);
    }

    public class QueryContextQueryType : ObjectType<QueryContextQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<QueryContextQuery> descriptor)
        {
            descriptor.Field(x => x.GetEmployees(null!));
        }
    }

    private static readonly Employee[] s_employees =
    [
        new()
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@acme.com"
        }
    ];
}
