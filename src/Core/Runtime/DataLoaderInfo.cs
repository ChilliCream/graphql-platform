using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Runtime
{
    public class DataLoaderInfo
    {
        private readonly DataLoaderDescriptor _descriptor;

        public DataLoaderInfo(DataLoaderDescriptor descriptor, object instance)
        {
            _descriptor = descriptor
                ?? throw new ArgumentNullException(nameof(descriptor));
            Instance = instance
                ?? throw new ArgumentNullException(nameof(instance));
            NeedsToBeTriggered = _descriptor.TriggerLoadAsync != null;
        }

        /// <summary>
        /// The data loader instance.
        /// </summary>
        public object Instance { get; }

        /// <summary>
        /// Defines if his data loader has to be triggered
        /// in order for it to load data.
        /// </summary>
        public bool NeedsToBeTriggered { get; }

        /// <summary>
        /// Signals the data loader that all data load registrations have
        /// been made an the batched data retrieval can be started.
        /// </summary>
        public Task TriggerAsync(CancellationToken cancellationToken)
        {
            if (NeedsToBeTriggered)
            {
                return _descriptor.TriggerLoadAsync(
                    Instance, cancellationToken);
            }
            return Task.CompletedTask;
        }
    }
}
