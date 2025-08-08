using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.DependencyInjection;

/// <summary>
/// <para>
/// The <see cref="ResultBuilderFactory"/> creates new instances of
/// <see cref="ResultBuilder"/>.
/// </para>
/// <para>The <see cref="ResultBuilderFactory"/> MUST be a singleton.</para>
/// </summary>
internal class ResultBuilderFactory : IFactory<ResultBuilder, ResultPool>
{
    public ResultBuilder Create(ResultPool input)
    {
        return new ResultBuilder(input);
    }
}
