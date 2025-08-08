using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution.DependencyInjection;

/// <summary>
/// <para>
/// The <see cref="ResultBuilderFactory"/> creates new instances of
/// <see cref="ResultBuilder"/>.
/// </para>
/// <para>The <see cref="ResultBuilderFactory"/> MUST be a singleton.</para>
/// </summary>
internal class ResultBuilderFactory : IFactory<IResultBuilder, ResultPool>
{
    public IResultBuilder Create(ResultPool input)
    {
        return new ResultBuilder(input);
    }
}
