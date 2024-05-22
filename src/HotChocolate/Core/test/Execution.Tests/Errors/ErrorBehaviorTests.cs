#nullable enable

using System.Diagnostics.CodeAnalysis;
using CookieCrumble;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.SnapshotHelpers;

namespace HotChocolate.Execution.Errors;

public class ErrorBehaviorTests
{
    [Fact]
    public async Task MatchTestSchema()
    {
        var executor = await CreateExecutorAsync();
        executor.Schema.MatchSnapshot();
    }
    
    [Fact]
    public async Task SyntaxError()
        => await MatchQueryAsync("{ error1");
    
    [Fact]
    public async Task AsyncMethod_NoAwait_Throw_ApplicationError()
        => await MatchQueryAsync("{ error1 }");

    [Fact]
    public async Task AsyncMethod_Await_Throw_ApplicationError()
        => await MatchQueryAsync("{ error4 }");

    [Fact]
    public async Task SyncMethod_Throw_ApplicationError()
        => await MatchQueryAsync("{ error7 }");

    [Fact]
    public async Task Property_Throw_ApplicationError()
        => await MatchQueryAsync("{ error10 }");

    [Fact]
    public async Task AsyncMethod_NoAwait_Throw_UnexpectedError()
        => await MatchQueryAsync("{ error2 }");

    [Fact]
    public async Task AsyncMethod_Await_Throw_UnexpectedError()
        => await MatchQueryAsync("{ error5 }");

    [Fact]
    public async Task SyncMethod_Throw_UnexpectedError()
        => await MatchQueryAsync("{ error8 }");

    [Fact]
    public async Task Property_Throw_UnexpectedError()
        => await MatchQueryAsync("{ error11 }");

    [Fact]
    public async Task AsyncMethod_NoAwait_Return_ApplicationError()
        => await MatchQueryAsync("{ error3 }");

    [Fact]
    public async Task AsyncMethod_Await_Return_ApplicationError()
        => await MatchQueryAsync("{ error6 }");


    [Fact]
    public async Task SyncMethod_Return_ApplicationError()
        => await MatchQueryAsync("{ error9 }");

    [Fact]
    public async Task Property_Return_ApplicationError()
        => await MatchQueryAsync("{ error12 }");

    [Fact]
    public async Task Property_Return_UnexpectedErrorWithPath()
        => await MatchQueryAsync("{ error13 }");
    
    [Fact]
    public async Task Error_On_NonNull_Root_Field()
        => await MatchQueryAsync("{ error15 }");
    
    [Fact]
    public Task RootFieldNotDefined()
        => MatchQueryAsync("query { doesNotExist }");

    [Fact]
    public async Task RootTypeNotDefined()
        => await MatchQueryAsync("mutation { doesNotExist }");

    [Fact] 
    public async Task Error_Filter_Adds_Details()
    {
        // arrange
        using var snapshot = StartResultSnapshot();
        
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddErrorFilter(error => error.SetExtension("foo", "bar"))
            .BuildRequestExecutorAsync();
        
        // act
        var result = await executor.ExecuteAsync("{ error14 }");

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async void Resolver_InvalidParentCast()
    {
        // arrange
        using var snapshot = StartResultSnapshot();
        
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Field("foo")
                .Type<ObjectType<Foo>>()
                .Extend()
                // in the pure resolver we will return the wrong type
                .Definition.Resolver = _ => new ValueTask<object?>(new Baz()))
            .BuildRequestExecutorAsync();
        
        // act
        var result = await executor.ExecuteAsync("{ foo { bar } }");

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async void PureResolver_InvalidParentCast()
    {
        // arrange
        using var snapshot = StartResultSnapshot();
        
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Field("foo")
                .Type<ObjectType<Foo>>()
                .Extend()
                // in the pure resolver we will return the wrong type
                .Definition.PureResolver = _ => new Baz())
            .BuildRequestExecutorAsync();
        
        // act
        var result = await executor.ExecuteAsync("{ foo { bar } }");

        // assert
        snapshot.Add(result);
    }

    [Fact]
    public async void SetMaxAllowedValidationErrors_To_1()
    {
        // arrange
        using var snapshot = StartResultSnapshot();
        
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Field("foo")
                .Type<ObjectType<Foo>>()
                .Extend()
                // in the pure resolver we will return the wrong type
                .Definition.PureResolver = _ => new Baz())
            .SetMaxAllowedValidationErrors(1)
            .BuildRequestExecutorAsync();
        
        // act
        var result = await executor.ExecuteAsync("{ a b c d }");

        // assert
        snapshot.Add(result);
    }

    private static async Task MatchQueryAsync(string query)
    {
        using var snapshot = StartResultSnapshot();

        var executor = await CreateExecutorAsync();
        var result = await executor.ExecuteAsync(query);

        snapshot.Add(result);
    }
    
    private static async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .BuildRequestExecutorAsync();
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.Error3()).Type<StringType>();
            descriptor.Field(t => t.Error6()).Type<StringType>();
            descriptor.Field(t => t.Error9()).Type<StringType>();
            descriptor.Field(t => t.Error12).Type<StringType>();
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class Query
    {
        public Task<string?> Error1()
        {
            throw new GraphQLException("query error 1");
        }

        public Task<string?> Error2()
        {
            throw new Exception("query error 2");
        }

        public Task<object?> Error3()
        {
            return Task.FromResult<object?>(
                ErrorBuilder.New()
                    .SetMessage("query error 3")
                    .Build());
        }

        public async Task<string?> Error4()
        {
            await Task.Delay(1);
            throw new GraphQLException("query error 4");
        }

        public async Task<string?> Error5()
        {
            await Task.Delay(1);
            throw new Exception("query error 5");
        }

        public async Task<object?> Error6()
        {
            await Task.Delay(1);
            return await Task.FromResult<object>(
                ErrorBuilder.New()
                    .SetMessage("query error 6")
                    .Build());
        }

        public string? Error7()
        {
            throw new GraphQLException("query error 7");
        }

        public string? Error8()
        {
            throw new Exception("query error 8");
        }

        public object? Error9()
        {
            return ErrorBuilder.New()
                .SetMessage("query error 9")
                .Build();
        }

        public string? Error10 => throw new GraphQLException("query error 10");

        public string? Error11 => throw new Exception("query error 11");

        public object? Error12 => ErrorBuilder.New()
            .SetMessage("query error 12")
            .Build();

        public Foo? Error13 => new Foo();

        public string? Error14 => throw new ArgumentNullException("Error14");
        
        public string Error15 => throw new ArgumentNullException("Error15");
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class Foo
    {
        public string? Bar => throw new Exception("baz");
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class Baz
    {
        public string? Bar => throw new Exception("baz");
    }
}