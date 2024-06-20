using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Shared;

public static class RequestExecutorBuilderTestExtensions
{
    public static IRequestExecutorBuilder AddResolverMocking(this IRequestExecutorBuilder builder)
    {
        return builder.UseField<MockFieldMiddleware>();
    }

    public static IRequestExecutorBuilder AddTestDirectives(this IRequestExecutorBuilder builder)
    {
        return builder
            .AddDirectiveType(new DirectiveType(
                d => d.Name("error")
                    .Location(DirectiveLocation.Field | DirectiveLocation.FieldDefinition)))
            .AddDirectiveType(new DirectiveType(
                d => d.Name("null")
                    .Location(DirectiveLocation.Field | DirectiveLocation.FieldDefinition)));
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class MockFieldMiddleware(FieldDelegate next)
    {
        public ValueTask InvokeAsync(IMiddlewareContext context)
        {
            var field = context.Selection.Field;

            if (field.Directives.ContainsDirective("error"))
            {
                context.ReportError($"Field \"{context.Selection.Field.Name}\" produced an error");
                context.Result = null;
                return ValueTask.CompletedTask;
            }

            if (field.Directives.ContainsDirective("null"))
            {
                context.Result = null;
                return ValueTask.CompletedTask;
            }

            var fieldType = field.Type.NamedType();

            if (fieldType is IObjectType)
            {
                context.Result = new object();
            }
            else
            {
                context.Result = fieldType switch
                {
                    IdType => "456",
                    StringType => "string",
                    IntType => 123,
                    FloatType => 123.456,
                    BooleanType => true,
                    _ => null,
                };
            }

            return ValueTask.CompletedTask;
        }
    }
}
