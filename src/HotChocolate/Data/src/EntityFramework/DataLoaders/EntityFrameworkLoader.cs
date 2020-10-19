using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.DataLoader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HotChocolate.Data.DataLoaders
{
    public class EntityFrameworkLoader<TKey, TData, TProp, TDb> : BatchDataLoader<TData, ICollection<TProp>>
        where TData : IModelId<TKey> where TProp : class, IModelId<TKey> where TDb : DbContext where TKey : class
    {
        private readonly IDbContextFactory<TDb> _dbContextFactory;

        private List<ISkipNavigation>? _skipNavigations;

        public EntityFrameworkLoader(
            IBatchScheduler batchScheduler,
            IDbContextFactory<TDb> dbContextFactory
        ) : base(batchScheduler)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        private List<ISkipNavigation> GetSkipNavigations(TDb context) =>
            _skipNavigations ??= context.Model.GetEntityTypes(typeof(TData)).SelectMany(et => et.GetSkipNavigations()).ToList();

        protected override async Task<IReadOnlyDictionary<TData, ICollection<TProp>>> LoadBatchAsync(
            IReadOnlyList<TData> keys, CancellationToken cancellationToken
        )
        {
            await using TDb db = _dbContextFactory.CreateDbContext();
            List<TData> data = keys.ToList();

            // TDB db = _dbFactory.Value;
            List<ISkipNavigation> navigations = GetSkipNavigations(db);
            ISkipNavigation? nav = navigations.FirstOrDefault(n => n.ClrType == typeof(ICollection<TProp>));
            if (nav == null)
            {
                throw new ArgumentException($"{typeof(TProp).Name} could not be resolved as a SkipNavigation on {typeof(TData).Name}");
            }

            if (nav.TargetEntityType.ClrType != typeof(TProp))
            {
                throw new ArgumentException($"{nav.Name} was a {nav.TargetEntityType.ClrType}, requested {typeof(TProp)}");
            }
            string joinDataKey = nav.ForeignKey.Properties.First().GetColumnBaseName();
            string joinPropKey = nav.Inverse.ForeignKey.Properties.First().GetColumnBaseName();

            List<TKey> dataKeys = data.Select(d => d.Id).ToList();

            // Query the intermediate table for all rows containing one of our data elements
            List<Dictionary<string, object>> join = await db.Set<Dictionary<string, object>>(nav.JoinEntityType.Name)
                                                            .Where(r => dataKeys.Contains(r[joinDataKey] as TKey))
                                                            .ToListAsync(cancellationToken);

            // Map the local data object IDs to a list of IDs they contain on the TProp
            Dictionary<object, List<object>> joinMap = join.GroupBy(r => r[joinDataKey]).ToDictionary(
                r => r.Key,
                r => r.Select(i => i[joinPropKey]).ToList());

            // Get all the unique IDs to query on the prop table
            List<object> propKeys = joinMap.Values.SelectMany(l => l).Distinct().ToList();
            Dictionary<object, TProp> values = await db.Set<TProp>()
                                                       .Where(p => propKeys.Contains(p.Id))
                                                       .ToDictionaryAsync(
                                                            m => m.Id as object,
                                                            m => m,
                                                            cancellationToken);

            return data.ToDictionary(
                p => p,
                p => ((joinMap.ContainsKey(p.Id) ? joinMap[p.Id] : new List<object>()).Select(k => values[k]).ToList()) as ICollection<TProp>);
        }
    }
}
