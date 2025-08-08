using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.DependencyInjection.Factories;

public class ResultBuilderFactoryTests
{
    [Fact]
    public void Create_Returns_ResultWithOfTypeResultBuilder()
    {
        var factory = new ResultBuilderFactory();
        var resultPool = new ResultPool(
            new ObjectResultPool(16, 16, 16),
            new ListResultPool(16, 16, 16)
        );

        var result = factory.Create(resultPool);

        Assert.IsType<ResultBuilder>(result);
    }
}
