using System.Buffers;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using CookieCrumble.Formatters;

namespace HotChocolate.Types;

public class SchemaFirstMutations
{
    [Fact]
    public async Task SimpleMutation_Inferred()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(@"
                    type Mutation {
                        doSomething(something: String) : String
                    }")
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions
                    {
                        ApplyToAllMutations = true,
                    })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_FieldOverride()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(@"
                    type Mutation {
                        doSomething(something: String) : String
                            @mutation(payloadFieldName: ""something"")
                    }")
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions
                    {
                        ApplyToAllMutations = true,
                    })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task MutationConventionDirective_ArgumentError_String()
    {
        async Task Error() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(@"
                    type Mutation {
                        doSomething(something: String) : String
                            @mutation(payloadFieldName: 123)
                    }")
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions
                    {
                        ApplyToAllMutations = true,
                    })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        var ex = await Assert.ThrowsAsync<SchemaException>(Error);
        ex.MatchSnapshot(formatter: new SchemaExceptionFormatter());
    }

    [Fact]
    public async Task MutationConventionDirective_ArgumentError_Boolean()
    {
        async Task Error() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(@"
                    type Mutation {
                        doSomething(something: String) : String
                            @mutation(enabled: 123)
                    }")
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions
                    {
                        ApplyToAllMutations = true,
                    })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        var ex = await Assert.ThrowsAsync<SchemaException>(Error);
        ex.MatchSnapshot(formatter: new SchemaExceptionFormatter());
    }

    [Fact]
    public async Task MutationConventionDirective_ArgumentUnknown()
    {
        async Task Error() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(@"
                    type Mutation {
                        doSomething(something: String) : String
                            @mutation(wrong: 123)
                    }")
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions
                    {
                        ApplyToAllMutations = true,
                    })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        var ex = await Assert.ThrowsAsync<SchemaException>(Error);
        ex.MatchSnapshot(formatter: new SchemaExceptionFormatter());
    }

    [Fact]
    public async Task MutationConventionDirective_WrongLocation()
    {
        async Task Error() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(@"
                    type Mutation {
                        doSomething(something: String @mutation) : String
                    }")
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions
                    {
                        ApplyToAllMutations = true,
                    })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        var ex = await Assert.ThrowsAsync<SchemaException>(Error);
        ex.MatchSnapshot(formatter: new SchemaExceptionFormatter());
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Execute()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(
                    @"
                    type Mutation {
                        doSomething(something: String) : String
                    }")
                .AddResolver(
                    "Mutation",
                    "doSomething",
                    ctx => ctx.ArgumentValue<string?>("something"))
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    @"mutation {
                        doSomething(input: { something: ""abc"" }) {
                            string
                        }
                    }");

        result.MatchSnapshot();
    }

    public class Mutation
    {
        public string? DoSomething(string? something)
            => something;
    }

    public class SchemaExceptionFormatter : ISnapshotValueFormatter
    {
        public bool CanHandle(object? value)
            => value is SchemaException;

        public void Format(IBufferWriter<byte> snapshot, object? value)
        {
            var ex = (SchemaException)value!;
            var next = false;

            foreach (var error in ex.Errors)
            {
                if (next)
                {
                    snapshot.AppendLine();
                }

                snapshot.Append(error.Message);
                snapshot.AppendLine();

                if (error.Code is not null)
                {
                    snapshot.Append("code: " + error.Code);
                    snapshot.AppendLine();
                }

                if (error.TypeSystemObject is not null)
                {
                    snapshot.Append("type: " + error.TypeSystemObject.Name);
                    snapshot.AppendLine();
                }

                next = true;
            }
        }
    }
}
