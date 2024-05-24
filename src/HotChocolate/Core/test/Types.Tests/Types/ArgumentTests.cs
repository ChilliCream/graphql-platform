#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

namespace HotChocolate.Types;

public class ArgumentTests
{
    [Fact]
    public async Task Integration_Collection_EnsureCorrectRuntimeType()
    {
        // https://github.com/ChilliCream/graphql-platform/issues/4281

        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .UseField<Middleware>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .Create()
                .SetDocument(@"{
                        arrayOfScalarsA: arrayOfScalars(values: 1)
                        arrayOfScalarsB: arrayOfScalars(values: [1, 2])
                        arrayOfObjectsA: arrayOfObjects(values: { bar: 1 }) { bar }
                        arrayOfObjectsB: arrayOfObjects(values: [{ bar: 1 }, { bar: 2 }]) { bar }
                        listOfScalarsA: listOfScalars(values: 1)
                        listOfScalarsB: listOfScalars(values: [1, 2])
                        listOfObjectsA: listOfObjects(values: { bar: 1 }) { bar }
                        listOfObjectsB: listOfObjects(values: [{ bar: 1 }, { bar: 2 }]) { bar }
                    }")
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class Query
    {
        public int[] ArrayOfScalars(int[] values) => values;

        public Foo[] ArrayOfObjects(Foo[] values) => values;

        public List<int> ListOfScalars(List<int> values) => values;

        public List<Foo> ListOfObjects(List<Foo> values) => values;
    }

    public class Foo
    {
        public int Bar { get; set; }
    }

    internal class Middleware
    {
        private readonly FieldDelegate _next;
        public Middleware(FieldDelegate next) => _next = next;

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            var arguments = context.Selection.Field.Arguments;
            foreach (var argument in arguments)
            {
                var value = context.ArgumentValue<object?>(argument.Name)!;
                var actualType = value.GetType();

                if (argument.RuntimeType != actualType)
                {
                    context.ReportError(
                        $"RuntimeType ({argument.RuntimeType}) not equal " +
                        $"to actual type ({actualType})");
                }

                if (context.Selection.Field.Name.StartsWith("array"))
                {
                    if (!argument.RuntimeType.IsArray)
                    {
                        context.ReportError(
                            "Field defined with array but ArgDeg saying " +
                            $"it's a {argument.RuntimeType}");
                    }

                    if (!actualType.IsArray)
                    {
                        context.ReportError(
                            "Field defined with array but actual type " +
                            $"is a {actualType}");
                    }
                }
            }

            await _next(context);
        }
    }
}
