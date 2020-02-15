using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;

namespace MarshmallowPie.Processing
{
    public interface IQueryValidationRule
    {
        string Name { get; }

        ValueTask<IEnumerable<Issue>> ValidateAsync(
            ISchema schema,
            string fileName,
            DocumentNode document,
            CancellationToken cancellationToken);
    }
}
