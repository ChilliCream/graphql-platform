using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Moq;
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
            var onEatMock = new Mock<Func<ToppingInput, bool>>();
            IQueryExecutor executor = CreateSchema(onEatMock.Object).MakeExecutable();
            const string Query = @"
                mutation {
                  eat(topping: { pickles: [{ butterPickle: { size: 5, complexAssigned: { value: 3 }, complexAssignedNull: null } }] })
                }";

            // act
            IExecutionResult result = await executor.ExecuteAsync(Query);

            // assert
            Assert.Empty(result.Errors);
            Verify(onEatMock);
        }

        [Fact]
        public async Task ShouldNotFailWithVariables()
        {
            // arrange
            var onEatMock = new Mock<Func<ToppingInput, bool>>();
            IQueryExecutor executor = CreateSchema(onEatMock.Object).MakeExecutable();
            const string Query = @"
                mutation a($input: ButterPickleInput!)
                {
                  eat(topping: { pickles: [{ butterPickle: $input }] })
                }";

            // act
            IExecutionResult result = await executor.ExecuteAsync(Query,
                new Dictionary<string, object>
                {
                    {
                        "input",
                        new Dictionary<string, object>
                        {
                            {"size", 5},
                            {"complexAssigned", new Dictionary<string, object> {{"value", 3}}},
                            {"complexAssignedNull", null}
                        }
                    }
                });

            // assert
            Assert.Empty(result.Errors);
            Verify(onEatMock);
        }

        private static void Verify(Mock<Func<ToppingInput, bool>> mock)
        {
            mock.Verify(x => x.Invoke(It.Is<ToppingInput>(t =>
                t.Pickles.First().ButterPickle!.Size == 5 && !t.Pickles.First().ButterPickle!.Width.HasValue &&
                !t.Pickles.First().ButterPickle!.ComplexUnassigned.HasValue &&
                t.Pickles.First().ButterPickle!.ComplexAssigned.HasValue &&
                t.Pickles.First().ButterPickle!.ComplexAssigned.Value!.Value == 3 &&
                t.Pickles.First().ButterPickle!.ComplexAssignedNull.HasValue &&
                t.Pickles.First().ButterPickle!.ComplexAssignedNull.Value == null)));
        }

        private static Schema CreateSchema(Func<ToppingInput, bool>? onEat = null)
        {
            return Schema.Create(s => s
                .RegisterQueryType<Query>()
                .RegisterMutationType(new MutationType(onEat)));
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
                    .Resolver(ctx => _onEat(ctx.Argument<ToppingInput>("topping")));
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
        }

        public class SomeComplexInput
        {
            public int Value { get; set; }
        }
    }
}
