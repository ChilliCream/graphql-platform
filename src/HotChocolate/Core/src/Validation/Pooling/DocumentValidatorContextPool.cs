using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Validation;

/// <summary>
/// A pool of <see cref="DocumentValidatorContext"/> instances.
/// </summary>
/// <param name="size">
/// The size of the pool. If not specified, the pool will be sized to the number of
/// processors on the machine multiplied by 2.
/// </param>
public class DocumentValidatorContextPool(int? size = null)
    : DefaultObjectPool<DocumentValidatorContext>(new Policy(), size ?? Environment.ProcessorCount * 2)
{
    private sealed class Policy : IPooledObjectPolicy<DocumentValidatorContext>
    {
        public DocumentValidatorContext Create() => new();

        public bool Return(DocumentValidatorContext obj)
        {
            obj.Clear();
            return true;
        }
    }
}
