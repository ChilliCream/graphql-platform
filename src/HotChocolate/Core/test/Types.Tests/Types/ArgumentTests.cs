#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class ArgumentTests
    {

        [Fact]
        public async Task Integration_Collection_EnsureCorrectRuntimeType()
        {
            // https://github.com/ChilliCream/hotchocolate/issues/4281

            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .UseField<Middleware>()
                .BuildRequestExecutorAsync();

            FieldCollection<ObjectField>? fields = executor.Schema.QueryType.Fields;

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder
                    .New()
                    .SetQuery(@"{
                        arrayOfScalarsA: arrayOfScalars(values: 1)
                        arrayOfScalarsB: arrayOfScalars(values: [1, 2])
                        arrayOfObjectsA: arrayOfObjects(values: { bar: 1 }) { bar }
                        arrayOfObjectsB: arrayOfObjects(values: [{ bar: 1 }, { bar: 2 }]) { bar }
                        listOfScalarsA: listOfScalars(values: 1)
                        listOfScalarsB: listOfScalars(values: [1, 2])
                        listOfObjectsA: listOfObjects(values: { bar: 1 }) { bar }
                        listOfObjectsB: listOfObjects(values: [{ bar: 1 }, { bar: 2 }]) { bar }
                    }")
                    .Create());

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
                IFieldCollection<IInputField> arguments = context.Selection.Field.Arguments;
                foreach (IInputField argument in arguments)
                {
                    var value = context.ArgumentValue<object?>(argument.Name)!;
                    Type actualType = value.GetType();

                    if (argument.RuntimeType != actualType)
                    {
                        throw new Exception();
                    }
                }

                await _next(context);
            }
        }
    }
}
