using CookieCrumble;
using HotChocolate.Execution.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class ArgumentCoercionHelperTests
{
    [Fact]
    public async Task CoerceArgumentValueFromLiteral()
    {
        int argumentValue;

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        var request = QueryRequestBuilder.New()
            .SetQuery("""
                      {
                        parent {
                          child(arg: SOME_VALUE)
                        }
                      }
                      """)
            .Create();

        var result = await executor.ExecuteAsync(request);

        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "parent": {
                                         "child": "SOME_VALUE"
                                       }
                                     }
                                   }
                                   """);
    }

    [Fact]
    public async Task CoerceArgumentValueFromVariable()
    {
        int argumentValue;

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        var request = QueryRequestBuilder.New()
            .SetQuery("""
                      query Test($variable: TestEnum!) {
                        parent {
                          child(arg: $variable)
                        }
                      }
                      """)
            .SetVariableValue("variable", "SOME_VALUE")
            .Create();

        var result = await executor.ExecuteAsync(request);

        result.MatchInlineSnapshot("""
                                   {
                                     "data": {
                                       "parent": {
                                         "child": "SOME_VALUE"
                                       }
                                     }
                                   }
                                   """);
    }

    public class Query
    {
        public Parent GetParent(IResolverContext context)
        {
            var selection = context.GetSelections((ObjectType)context.Selection.Type).First();

            if (selection.Arguments.TryCoerceArguments(context, out var args) &&
                args.TryGetValue("arg", out var arg) &&
                arg.TryCoerceValue<TestEnum>(context, out var argValue))
            {
                return new(argValue);
            }

            return null;
        }
    }

    public class Parent(TestEnum childArgumentValue)
    {
        public TestEnum Child(TestEnum arg) => childArgumentValue;
    }

    public enum TestEnum
    {
        SomeValue
    }
}
