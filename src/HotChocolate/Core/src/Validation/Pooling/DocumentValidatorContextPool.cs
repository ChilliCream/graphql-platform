using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Validation
{
    public class DocumentValidatorContextPool
        : DefaultObjectPool<DocumentValidatorContext>
    {
        public DocumentValidatorContextPool(int size = 8)
            : base(new Policy(), size)
        {
        }

        private class Policy : IPooledObjectPolicy<DocumentValidatorContext>
        {
            public DocumentValidatorContext Create() => new DocumentValidatorContext();

            public bool Return(DocumentValidatorContext obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
