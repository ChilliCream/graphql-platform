using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class Issue6605ReproTests
{
    [Fact]
    public async Task Mutation_Error_Type_With_Int_Field_Should_Not_Fail_Schema_Build()
    {
        var exception = await Record.ExceptionAsync(async () =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddMutationConventions()
                .AddMutationErrorConfiguration<CustomErrorConfig>()
                .BuildSchemaAsync());

        Assert.Null(exception);
    }

    public class CustomErrorConfig : MutationErrorConfiguration
    {
        public override void OnConfigure(
            IDescriptorContext context,
            ObjectFieldConfiguration mutationField)
        {
            mutationField.AddErrorType(context, typeof(CustomError));
            mutationField.MiddlewareConfigurations.Add(
                new(next => async ctx =>
                {
                    try
                    {
                        await next(ctx);
                    }
                    catch (Exception ex)
                    {
                        ctx.Result = new FieldError(new CustomError(1, ex.Message));
                    }
                }));
        }
    }

    public record CustomError(int ErrorCode, string Message);

    public class Query
    {
        public Foo GetFoo() => new("Baz");
    }

    public class Mutation
    {
        public Foo AddFoo() => throw new Exception("foo");
    }

    public record Foo(string Bar);
}
