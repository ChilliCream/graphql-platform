using System;
using System.Collections.Generic;
using System.Globalization;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Requests
{
    public class StitchingContext : IStitchingContext
    {
        private readonly Dictionary<NameString, RemoteRequestExecutor> _executors =
            new Dictionary<NameString, RemoteRequestExecutor>();

        public StitchingContext(
            IBatchScheduler batchScheduler,
            IRequestContextAccessor requestContextAccessor)
        {
            if (batchScheduler is null)
            {
                throw new ArgumentNullException(nameof(batchScheduler));
            }

            if (requestContextAccessor is null)
            {
                throw new ArgumentNullException(nameof(requestContextAccessor));
            }

            foreach (KeyValuePair<NameString, IRequestExecutor> executor in
                requestContextAccessor.RequestContext.Schema.GetRemoteExecutors())
            {
                _executors.Add(
                    executor.Key,
                    new RemoteRequestExecutor(
                        batchScheduler,
                        executor.Value));
            }
        }

        public IRemoteRequestExecutor GetRemoteRequestExecutor(NameString schemaName)
        {
            schemaName.EnsureNotEmpty(nameof(schemaName));

            if (_executors.TryGetValue(schemaName, out RemoteRequestExecutor? executor))
            {
                return executor;
            }

            throw new ArgumentException(string.Format(
                CultureInfo.InvariantCulture,
                StitchingResources.SchemaName_NotFound,
                schemaName));
        }

        public ISchema GetRemoteSchema(NameString schemaName) =>
            GetRemoteRequestExecutor(schemaName).Schema;
    }
}
