using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

#nullable enable

namespace HotChocolate.Regressions
{
    // Relates to issue https://github.com/ChilliCream/hotchocolate/issues/2114
    public class NestedOptionalInt_2114
    {
        [Fact]
        public async Task ShouldNotFailWithExplicitValues()
        {
            // arrange
            ToppingInput? input = null;
            IRequestExecutor executor = CreateExecutor(value =>
            {
                input = value;
                return true;
            });

            const string Query = @"
                mutation {
                    eat(topping: {
                        pickles: [ {
                            butterPickle: {
                                size: 5,
                                complexAssigned: { value: 3 },
                                complexAssignedNull: null, complexList: [ { value: 2 } ] } } ] })
                }";
            // act
            IExecutionResult result = await executor.ExecuteAsync(Query);

            // assert
            Assert.Null(result.Errors);
            Verify(input);
        }

        [Fact]
        public async Task ShouldNotFailWithVariables()
        {
            // arrange
            ToppingInput? input = null;
            IRequestExecutor executor = CreateExecutor(value =>
            {
                input = value;
                return true;
            });

            const string Query = @"
                mutation a($input: ButterPickleInput!)
                {
                    eat(topping: {
                        pickles: [
                            { butterPickle: $input }
                        ] } )
                }";

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                Query,
                new Dictionary<string, object?>
                {
                    {
                        "input",
                        new Dictionary<string, object?>
                        {
                            { "size", 5 },
                            {
                                "complexAssigned",
                                new Dictionary<string, object?>
                                {
                                    { "value", 3 }
                                }
                            },
                            { "complexAssignedNull", null} ,
                            {
                                "complexList",
                                new List<Dictionary<string, object?>>
                                {
                                    new Dictionary<string, object?> { { "value", 2 } }
                                }
                            }
                        }
                    }
                });

            // assert
            Assert.Null(result.Errors);
            Verify(input);
        }

        private static void Verify(ToppingInput? input)
        {
            Assert.NotNull(input);
            ButterPickleInput? pickle = input?.Pickles!.First()?.ButterPickle;
            Assert.NotNull(pickle);
            Assert.Equal(5, pickle?.Size);
            Assert.False(pickle?.Width.HasValue);
            Assert.False(pickle?.ComplexUnassigned.HasValue);
            Assert.True(pickle?.ComplexAssigned.HasValue);
            Assert.Equal(3, pickle?.ComplexAssigned.Value?.Value);
            Assert.True(pickle?.ComplexAssignedNull.HasValue);
            Assert.Null(pickle?.ComplexAssignedNull.Value);
            Assert.True(pickle?.ComplexList.HasValue);
            Assert.Equal(2, pickle?.ComplexList.Value?.First().Value);
        }

        private static IRequestExecutor CreateExecutor(Func<ToppingInput, bool>? onEat = null)
        {
            return SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddMutationType(new MutationType(onEat))
                .Create()
                .MakeExecutable();
        }

        public class Query
        {
            public string Chocolate => "rain";
        }

        public class MutationType : ObjectType
        {
            private readonly Func<ToppingInput, bool> _onEat;

            public MutationType(Func<ToppingInput, bool>? onEat = null)
            {
                _onEat = onEat ?? (_ => true);
            }

            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Field("eat")
                    .Type<NonNullType<BooleanType>>()
                    .Argument("topping", arg => arg.Type(typeof(ToppingInput)))
                    .Resolver(ctx => _onEat(ctx.ArgumentValue<ToppingInput>("topping")));
            }
        }

        public class ToppingInput
        {
            public IEnumerable<PicklesInput>? Pickles { get; set; }
        }

        public class PicklesInput
        {
            public ButterPickleInput? ButterPickle { get; set; }
        }

        public class ButterPickleInput
        {
            public Optional<int> Size { get; set; }
            public Optional<int?> Width { get; set; }
            public Optional<SomeComplexInput?> ComplexUnassigned { get; set; }
            public Optional<SomeComplexInput?> ComplexAssigned { get; set; }
            public Optional<SomeComplexInput?> ComplexAssignedNull { get; set; }
            public Optional<List<SomeComplexInput>?> ComplexList { get; set; }
        }

        public class SomeComplexInput
        {
            public int Value { get; set; }
        }
    }
}
