using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.ConferencePlanner.Data;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.ConferencePlanner.DataLoader
{
    public class SpeakerBySessionIdDataLoader : GroupedDataLoader<int, Speaker>
    {
        private static readonly string _speakerCacheKey = GetCacheKeyType<SpeakerByIdDataLoader>();
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public SpeakerBySessionIdDataLoader(
            IDbContextFactory<ApplicationDbContext> dbContextFactory,
            IBatchScheduler batchScheduler,
            DataLoaderOptions options)
            : base(batchScheduler, options)
        {
            _dbContextFactory = dbContextFactory ??
                throw new ArgumentNullException(nameof(dbContextFactory));
        }

        protected override async Task<ILookup<int, Speaker>> LoadGroupedBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            await using var dbContext =
                _dbContextFactory.CreateDbContext();

            var list = await dbContext.Sessions
                .Where(s => keys.Contains(s.Id))
                .Include(s => s.SessionSpeakers)
                .SelectMany(s => s.SessionSpeakers)
                .Include(s => s.Speaker)
                .ToListAsync(cancellationToken);

            TryAddToCache(_speakerCacheKey, list, item => item.SpeakerId, item => item.Speaker!);

            return list.ToLookup(t => t.SessionId, t => t.Speaker!);
        }
    }
}
