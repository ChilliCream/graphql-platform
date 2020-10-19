using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.DataLoaders
{
    public class EntityFrameworkResolver<TKey, TData, TProp, TDb>
        where TData : IModelId<TKey> where TProp : class, IModelId<TKey> where TDb : DbContext where TKey : class {
        public Task<ICollection<TProp>> ManyToMany(
            TData data,
            EntityFrameworkLoader<TKey, TData, TProp, TDb> loader,
            CancellationToken cancellationToken
        ) => loader.LoadAsync(data, cancellationToken);
    }
}
