using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination;

public class MockExecutable<T> : IExecutable<T>
{
    private readonly IQueryable<T> _source;

    public MockExecutable(IQueryable<T> source)
    {
        _source = source;
    }

    public object Source => _source;

    public ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
    {
        return new(_source.ToList());
    }

    public ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
    {
        return new(_source.FirstOrDefault());
    }

    public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
    {
        return new(_source.SingleOrDefault());
    }

    public string Print()
    {
        return _source.ToString()!;
    }
}