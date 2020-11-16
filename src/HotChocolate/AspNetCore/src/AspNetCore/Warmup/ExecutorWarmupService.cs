using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Warmup
{
    internal class ExecutorWarmupService : BackgroundService
    {
        private readonly IRequestExecutorResolver _executorResolver;
        private readonly HashSet<NameString> _schemaNames;

        public ExecutorWarmupService(
            IRequestExecutorResolver executorResolver,
            IEnumerable<WarmupSchema> schemas)
        {
            if (executorResolver is null!)
            {
                throw new ArgumentNullException(nameof(executorResolver));
            }

            if (schemas is null)
            {
                throw new ArgumentNullException(nameof(schemas));
            }

            _executorResolver = executorResolver;
            _schemaNames = new HashSet<NameString>(schemas.Select(t => t.SchemaName));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (NameString schemaName in _schemaNames)
            {
                await _executorResolver
                    .GetRequestExecutorAsync(schemaName, stoppingToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
