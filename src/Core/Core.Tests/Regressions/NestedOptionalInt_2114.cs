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
                  eat(topping: { pickles: [{ butterPickle: { size: 5 } }] })
                }";

            // act
            IExecutionResult result = await executor.ExecuteAsync(Query);

            // assert
            Assert.Empty(result.Errors);
            onEatMock.Verify(x => x.Invoke(It.Is<ToppingInput>(t =>
                t.Pickles.First().ButterPickle!.Size == 5 && !t.Pickles.First().ButterPickle!.Width.HasValue)));
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
                    {"input", new Dictionary<string, object> { {"size", 5} } }
                });

            // assert
            Assert.Empty(result.Errors);
            onEatMock.Verify(x => x.Invoke(It.Is<ToppingInput>(t =>
                t.Pickles.First().ButterPickle!.Size == 5 && !t.Pickles.First().ButterPickle!.Width.HasValue)));
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
        }
    }
}
